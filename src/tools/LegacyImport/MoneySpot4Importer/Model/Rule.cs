using System.Text.RegularExpressions;

namespace MoneySpot4Importer.Model
{
    public class Rule
    {
        public Rule()
        {
            Purpose = new StringFilter();
            ContraAccount = new StringFilter();
        }

        public Account Account { get; set; }
        public StringFilter Purpose { get; set; }
        public StringFilter ContraAccount { get; set; }
        public string Name { get; set; }
        public string Script { get; set; }
    }

    public class StringFilter
    {
        public StringFilter()
        {
            Value = String.Empty;
            IsRegEx = false;
        }

        public StringFilter(string val, bool isRegEx)
        {
            Value = val;
            IsRegEx = isRegEx;
        }

        public string Value { get; set; }
        public bool IsRegEx { get; set; }
    }
}