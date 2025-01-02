using System.Diagnostics;

namespace MoneySpot4Importer.Model
{
    [DebuggerDisplay("{Description}")]
    public class Booking
    {
        public Booking()
        {
            RawData = new BookingDetails();
            RuleResults = new RuleResult[0];
            Result = new BookingDetails();
            ManualOverrides = new BookingDetailsOverride();
        }

        public int SeqId { get; set; }
        public BookingDetails RawData { get; set; }
        public RuleResult[] RuleResults { get; set; }
        public BookingDetailsOverride ManualOverrides { get; set; }
        public BookingDetails Result { get; set; }
        public Account Account { get; set; }
        public bool IsNew { get; set; }

        public string Description
        {
            get { return string.Format("{0} - {1} - {2}", Result.Date.ToString("yyyy-MM-dd"), Result.Amount.ToString("0,000.00"), Result.Purpose); }
        }
    }

    public class RuleResult
    {
        public Rule Rule { get; set; }
        public BookingDetailsOverride Override { get; set; }
        public string ExceptionMessage { get; set; }
    }
}