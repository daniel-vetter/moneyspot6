namespace MoneySpot4Importer.Model
{
    public class ColorEntry
    {
        public string Color { get; set; }
        public string Value { get; set; }
        public ColorEntryType Type { get; set; }
    }

    public enum ColorEntryType
    {
        Category,
        ContraAccount,
        Purpose
    }
}