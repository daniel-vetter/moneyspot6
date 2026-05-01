using System.Text.RegularExpressions;
using Shouldly;

namespace MoneySpot6.E2eTests;

// Lightweight test that only reads README.md — no Docker setup, runs in milliseconds.
// Kept in the e2e project because that is where the constants live.
public class ReadmeDriftGuardTests
{
    [Test]
    public async Task Readme_contains_documented_docker_run_commands()
    {
        var readmePath = Path.Combine(DockerImageHelpers.ProjectRoot, "README.md");
        var readme = await File.ReadAllTextAsync(readmePath);

        // Collapse bash line continuations ("\<newline><indent>") to a single space
        // so a multi-line docker command in markdown matches our single-line constant.
        var normalized = Regex.Replace(readme, @" \\\r?\n\s+", " ");

        normalized.ShouldContain(ReadmeCommands.SqliteDockerRunCommand);
        normalized.ShouldContain(ReadmeCommands.PostgresDockerRunCommand);
    }
}
