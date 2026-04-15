using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Shouldly;

namespace MoneySpot6.E2eTests;

public class SelfUpdateE2eTests : PageTest
{
    private static DockerClient _client = null!;
    private static int _registryPort;
    private static int _appPort;
    private static string _appContainerName = null!;
    private static string _imageRef = null!;
    private static string _projectRoot = null!;
    private static readonly List<string> ContainersToCleanup = [];

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = $"http://localhost:{_appPort}"
    };

    [OneTimeSetUp]
    public static async Task Setup()
    {
        _client = new DockerClientConfiguration().CreateClient();
        _projectRoot = FindProjectRoot();

        var testId = Guid.NewGuid().ToString("N")[..8];
        _appContainerName = $"e2e-app-{testId}";

        Console.WriteLine("Starting registry...");
        await StartRegistry();
        Console.WriteLine($"Registry running on port {_registryPort}");

        _imageRef = $"localhost:{_registryPort}/moneyspot6:dev";

        var prebuiltImage = Environment.GetEnvironmentVariable("E2E_PREBUILT_IMAGE");
        if (prebuiltImage != null)
        {
            Console.WriteLine($"Using pre-built image: {prebuiltImage}");
            await TagImage(prebuiltImage, _imageRef);
        }
        else
        {
            Console.WriteLine("Building v1...");
            await DockerBuild(_imageRef, buildVersion: "v1");
        }
        Console.WriteLine("Pushing v1...");
        await DockerPush(_imageRef);

        _appPort = GetFreePort();

        var created = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = _imageRef,
            Name = _appContainerName,
            Env = ["Auth__Disable=true"],
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["80/tcp"] = [new PortBinding { HostPort = _appPort.ToString() }]
                },
                Binds = ["/var/run/docker.sock:/var/run/docker.sock"]
            }
        });
        ContainersToCleanup.Add(_appContainerName);

        await _client.Containers.StartContainerAsync(created.ID, new ContainerStartParameters());
        Console.WriteLine($"Container 80/tcp mapped to host port {_appPort}");

        Console.WriteLine($"App starting on port {_appPort}, waiting...");
        await WaitForApp();
        Console.WriteLine("App ready.");
    }

    [OneTimeTearDown]
    public static async Task Teardown()
    {
        foreach (var name in ContainersToCleanup)
        {
            try { await _client.Containers.StopContainerAsync(name, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }); } catch { }
            try { await _client.Containers.RemoveContainerAsync(name, new ContainerRemoveParameters { Force = true }); } catch { }
        }
        _client.Dispose();
    }

    [Test]
    public async Task Self_update_detects_and_applies_update_via_ui()
    {
        var v1Inspection = await _client.Containers.InspectContainerAsync(_appContainerName);
        var v1ImageId = v1Inspection.Image;

        Console.WriteLine("Building v2...");
        await DockerBuild(_imageRef, buildVersion: "v2");
        Console.WriteLine("Pushing v2...");
        await DockerPush(_imageRef);
        Console.WriteLine("v2 pushed.");

        // Navigate to system settings
        await Page.GotoAsync("/settings/system");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click "Check for updates"
        await Page.GetByRole(AriaRole.Button, new() { Name = "Jetzt prüfen" }).ClickAsync();

        // Wait for "Update available" message
        await Expect(Page.GetByText("Ein Update ist verfügbar.")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        // Click "Install update"
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update installieren" }).ClickAsync();
        Console.WriteLine("Update triggered, waiting for container restart...");

        // Wait for the sidecar to replace the container.
        // The old container gets stopped, then a new one starts with the new image.
        // We poll until we see a different image ID on the container.
        var deadline = DateTime.UtcNow.AddSeconds(120);
        string? v2ImageId = null;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(2000);
            try
            {
                var info = await _client.Containers.InspectContainerAsync(_appContainerName);
                if (info.State.Running && info.Image != v1ImageId)
                {
                    v2ImageId = info.Image;
                    Console.WriteLine($"Container restarted with new image: {v2ImageId}");
                    break;
                }
            }
            catch
            {
                // Container might be temporarily gone during restart
            }
        }

        v2ImageId.ShouldNotBeNull("Container should have been restarted with a new image within 120s");
        v2ImageId.ShouldNotBe(v1ImageId, "Container should be running a different image after update");

        // Wait for the new app to be ready (it will clean up the sidecar on startup)
        await WaitForApp();

        // Wait for the background worker to clean up the sidecar container
        var sidecarDeadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < sidecarDeadline)
        {
            var sidecarContainers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["label"] = new Dictionary<string, bool> { ["moneyspot6.sidecar=update"] = true }
                }
            });
            if (sidecarContainers.Count == 0) break;
            await Task.Delay(1000);
        }

        // Verify the update log was persisted by checking the UI
        await Page.GotoAsync("/settings/system");
        var logsButton = Page.GetByRole(AriaRole.Button, new() { Name = "Update-Logs anzeigen" });
        await Expect(logsButton).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await logsButton.ClickAsync();
        await Expect(Page.GetByText("Update complete")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    private static async Task StartRegistry()
    {
        await PullImage("registry:2");

        var registry = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "registry:2",
            Name = $"e2e-registry-{Guid.NewGuid():N}",
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["5000/tcp"] = [new PortBinding { HostPort = "0" }]
                }
            }
        });
        ContainersToCleanup.Add(registry.ID);

        await _client.Containers.StartContainerAsync(registry.ID, new ContainerStartParameters());

        var inspection = await _client.Containers.InspectContainerAsync(registry.ID);
        _registryPort = int.Parse(inspection.NetworkSettings.Ports["5000/tcp"][0].HostPort);

        using var http = new HttpClient();
        for (var i = 0; i < 30; i++)
        {
            try
            {
                var response = await http.GetAsync($"http://localhost:{_registryPort}/v2/");
                if (response.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(500);
        }
        throw new Exception("Registry did not become ready");
    }

    private static async Task WaitForApp()
    {
        using var http = new HttpClient();
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var response = await http.GetAsync($"http://localhost:{_appPort}/");
                if (response.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(2000);
        }
        throw new Exception("App did not become ready");
    }

    private static async Task DockerBuild(string tag, string buildVersion)
    {
        var result = await RunProcess("docker",
            $"build -t {tag} --build-arg BUILD_VERSION={buildVersion} .",
            _projectRoot);
        result.ExitCode.ShouldBe(0, $"docker build failed:\n{result.Output}");
    }

    private static async Task DockerPush(string tag)
    {
        var result = await RunProcess("docker", $"push {tag}");
        result.ExitCode.ShouldBe(0, $"docker push failed:\n{result.Output}");
    }

    private static async Task TagImage(string source, string target)
    {
        var result = await RunProcess("docker", $"tag {source} {target}");
        result.ExitCode.ShouldBe(0, $"docker tag failed:\n{result.Output}");
    }

    private static async Task PullImage(string image)
    {
        var parts = image.Split(':');
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = parts[0], Tag = parts.Length > 1 ? parts[1] : "latest" },
            null,
            new Progress<JSONMessage>());
    }

    private static async Task<ProcessResult> RunProcess(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
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

        return new ProcessResult(process.ExitCode, $"{await stdoutTask}\n{await stderrTask}");
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

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private record ProcessResult(int ExitCode, string Output);
}
