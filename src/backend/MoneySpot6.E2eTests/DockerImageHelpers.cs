using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MoneySpot6.E2eTests;

internal static class DockerImageHelpers
{
    public static string ProjectRoot { get; } = FindProjectRoot();

    // Either tags a CI-provided prebuilt image as the requested tag,
    // or builds the image from the project's Dockerfile.
    public static async Task EnsureImage(string targetTag, string? buildVersion = null)
    {
        var prebuilt = Environment.GetEnvironmentVariable("E2E_PREBUILT_IMAGE");
        if (prebuilt != null)
            await RunDocker($"tag {prebuilt} {targetTag}", expectedExitCode: 0);
        else
            await BuildImage(targetTag, buildVersion);
    }

    public static async Task BuildImage(string targetTag, string? buildVersion = null)
    {
        var buildArg = buildVersion != null ? $" --build-arg BUILD_VERSION={buildVersion}" : "";
        await RunDocker($"build -t {targetTag}{buildArg} .", ProjectRoot, expectedExitCode: 0);
    }

    // Invokes docker.exe directly with the args portion. Bypasses the system
    // shell so cmd.exe vs /bin/sh quoting differences are out of the picture —
    // docker handles its own (POSIX-style) argument parsing consistently.
    // Pass expectedExitCode to throw automatically on mismatch.
    public static async Task<ProcessResult> RunDocker(string args, string? workingDirectory = null, int? expectedExitCode = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (workingDirectory != null)
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = new ProcessResult(process.ExitCode, $"{await stdoutTask}\n{await stderrTask}");
        if (expectedExitCode.HasValue && result.ExitCode != expectedExitCode.Value)
            throw new Exception($"`docker {args}` exited with {result.ExitCode}, expected {expectedExitCode.Value}:\n{result.Output}");
        return result;
    }

    public static async Task<string> TryReadContainerLogs(string containerName)
    {
        try
        {
            var logs = await RunDocker($"logs --tail 100 {containerName}");
            return logs.Output;
        }
        catch (Exception ex)
        {
            return $"(could not read logs: {ex.Message})";
        }
    }

    public static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindProjectRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Dockerfile")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new Exception("Could not find project root (no Dockerfile found)");
    }

    public record ProcessResult(int ExitCode, string Output);
}
