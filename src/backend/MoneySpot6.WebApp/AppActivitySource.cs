using System.Diagnostics;

namespace MoneySpot6.WebApp;

class AppActivitySource
{
    private static readonly ActivitySource Source = new(Name);
    
    public static string Name => "MoneySpot6.WebApp";
    
    public static Activity? Start(string name) => Source.StartActivity(name) ?? throw new Exception("Could not start Activity");
}