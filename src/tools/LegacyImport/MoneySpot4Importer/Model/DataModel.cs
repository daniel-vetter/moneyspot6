namespace MoneySpot4Importer.Model
{
    public class DataModel
    {
        public DataModel()
        {
            Accounts = new List<Account>();
            Rules = new List<Rule>();
            Bookings = new List<Booking>();
            Colors = new List<ColorEntry>();
        }

        public List<Account> Accounts { get; set; }

        public List<Rule> Rules { get; set; }

        public List<Booking> Bookings { get; set; }

        public List<ColorEntry> Colors { get; set; }
    }
}