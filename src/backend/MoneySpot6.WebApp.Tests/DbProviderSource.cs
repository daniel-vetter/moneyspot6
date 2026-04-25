using System.Collections;

namespace MoneySpot6.WebApp.Tests;

public enum DbProvider
{
    Sqlite,
    Postgres
}

public class DbProviderSource : IEnumerable
{
    public IEnumerator GetEnumerator() => Enum.GetValues<DbProvider>().GetEnumerator();
}
