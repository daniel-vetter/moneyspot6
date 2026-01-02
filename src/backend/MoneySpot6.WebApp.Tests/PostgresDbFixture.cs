using Testcontainers.PostgreSql;

namespace MoneySpot6.WebApp.Tests;

[SetUpFixture]
public class PostgresDbFixture
{
    private static PostgreSqlContainer _postgres = null!;

    public static string ConnectionString => _postgres.GetConnectionString();

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgres.StartAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        await _postgres.DisposeAsync();
    }
}
