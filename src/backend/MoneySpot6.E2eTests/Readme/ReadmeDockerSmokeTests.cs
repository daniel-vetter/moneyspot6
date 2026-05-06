using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using static MoneySpot6.E2eTests.DockerImageHelpers;

namespace MoneySpot6.E2eTests;

public class ReadmeDockerSmokeTests : PageTest
{
    private const string SqliteVolumeName = "moneyspot6-data";

    private static string _imageRef = null!;
    private readonly List<string> _testContainers = [];
    private readonly List<string> _testNetworks = [];

    [OneTimeSetUp]
    public static async Task Setup()
    {
        _imageRef = $"moneyspot6-readme-smoke:{Guid.NewGuid():N}";
        await EnsureImage(_imageRef);
    }

    [OneTimeTearDown]
    public static async Task FixtureTeardown()
    {
        await RunDocker($"volume rm -f {SqliteVolumeName}");
    }

    [TearDown]
    public async Task TestTeardown()
    {
        foreach (var name in _testContainers)
            await RunDocker($"rm -f {name}");
        foreach (var name in _testNetworks)
            await RunDocker($"network rm {name}");
        _testContainers.Clear();
        _testNetworks.Clear();
    }

    [Test]
    public async Task Sqlite_command_starts_app_and_renders_dashboard()
    {
        var hostPort = GetFreePort();
        var containerName = $"smoke-sqlite-{Guid.NewGuid():N}"[..16];
        _testContainers.Add(containerName);

        // Insert --name and substitute port + image so the literal command runs against the local build on a free port.
        var command = ReadmeCommands.SqliteDockerRunCommand
            .Replace("-p 80:80", $"-p {hostPort}:80")
            .Replace("dvetter/moneyspot6", $"--name {containerName} {_imageRef}");

        await RunDocker(command["docker ".Length..], expectedExitCode: 0);

        await AssertDashboardRenders(hostPort, containerName);
    }

    [Test]
    public async Task Postgres_command_starts_app_and_renders_dashboard()
    {
        var hostPort = GetFreePort();
        var testId = Guid.NewGuid().ToString("N")[..8];
        var networkName = $"smoke-pg-net-{testId}";
        var pgContainerName = $"smoke-pg-{testId}";
        var appContainerName = $"smoke-pg-app-{testId}";

        _testNetworks.Add(networkName);
        await RunDocker($"network create {networkName}", expectedExitCode: 0);

        // Postgres in the network with alias "myserver" so the unmodified
        // README connection string ("Host=myserver;...") resolves.
        _testContainers.Add(pgContainerName);
        await RunDocker(
            $"run -d --network {networkName} --network-alias myserver --name {pgContainerName} " +
            $"-e POSTGRES_DB=moneyspot -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=secret " +
            $"postgres:17-alpine",
            expectedExitCode: 0);
        await WaitForPostgres(pgContainerName);

        _testContainers.Add(appContainerName);

        // Insert --network/--name and substitute port + image so the literal command runs against the local build on a free port.
        var command = ReadmeCommands.PostgresDockerRunCommand
            .Replace("-p 80:80", $"-p {hostPort}:80")
            .Replace("dvetter/moneyspot6", $"--network {networkName} --name {appContainerName} {_imageRef}");

        await RunDocker(command["docker ".Length..], expectedExitCode: 0);

        await AssertDashboardRenders(hostPort, appContainerName);
    }

    private async Task AssertDashboardRenders(int port, string containerName)
    {
        // Wait for the app to respond before driving Playwright — Page.GotoAsync
        // does not retry on connection refused.
        await WaitForHttpReady(port, containerName);

        await Page.GotoAsync($"http://localhost:{port}/");

        // Fresh container shows the welcome screen first; pick the empty-start path.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Nein, leer starten" })
            .ClickAsync(new() { Timeout = 30_000 });

        // Dashboard rendered when the total amount shows 0,00 €.
        await Expect(Page.GetByTestId("total")).ToContainTextAsync("0,00", new() { Timeout = 30_000 });
    }

    private static async Task WaitForHttpReady(int port, string containerName)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTime.UtcNow.AddSeconds(120);
        HttpResponseMessage? lastResponse = null;
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                lastResponse = await http.GetAsync($"http://localhost:{port}/");
                if (lastResponse.IsSuccessStatusCode) return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
            await Task.Delay(2000);
        }

        var logs = await TryReadContainerLogs(containerName);
        throw new Exception(
            $"App on port {port} did not respond within 120s. " +
            $"Last status: {lastResponse?.StatusCode.ToString() ?? "no response"}, " +
            $"last exception: {lastException?.Message ?? "none"}.\n--- Container logs ---\n{logs}");
    }

    private static async Task WaitForPostgres(string containerName)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var result = await RunDocker($"exec {containerName} pg_isready -U postgres -d moneyspot");
            if (result.ExitCode == 0) return;
            await Task.Delay(1000);
        }
        throw new Exception($"Postgres container {containerName} did not become ready");
    }
}
