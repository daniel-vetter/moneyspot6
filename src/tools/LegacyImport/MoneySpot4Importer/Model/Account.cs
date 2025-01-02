namespace MoneySpot4Importer.Model
{
    public class Account
    {
        public object ImporterOptions { get; set; }
        public string ImporterKey { get; set; }
        public decimal Balance { get; set; }
        public string Name { get; set; }
    }
}