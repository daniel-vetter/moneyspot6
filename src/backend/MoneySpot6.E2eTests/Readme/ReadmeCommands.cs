namespace MoneySpot6.E2eTests;

// Single source of truth for the docker run commands documented in README.md.
// The drift-guard test asserts these strings appear verbatim in the README,
// so the README and tests cannot diverge silently.
internal static class ReadmeCommands
{
    public const string SqliteDockerRunCommand =
        "docker run -d --restart unless-stopped -p 80:80 -v moneyspot6-data:/app/data dvetter/moneyspot6";

    public const string PostgresDockerRunCommand =
        "docker run -d --restart unless-stopped -p 80:80 " +
        "-e ConnectionStrings__db=\"Host=myserver;Database=moneyspot;Username=postgres;Password=secret\" " +
        "dvetter/moneyspot6";
}
