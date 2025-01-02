namespace MoneySpot4Importer.Model
{
    public class BookingDetailsBase : IEquatable<BookingDetailsBase>
    {
        public string ContraAccountName { get; set; }
        public string SubAccount { get; set; }
        public string Purpose { get; set; }
        public string Currency { get; set; }
        public string ContraAccountBankCode { get; set; }
        public string ContraAccountNumber { get; set; }
        public string EndToEndId { get; set; }
        public string MandateId { get; set; }
        public string CreditorId { get; set; }
        public string Category { get; set; }

        public bool Equals(BookingDetailsBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(ContraAccountName, other.ContraAccountName) && string.Equals(SubAccount, other.SubAccount) && string.Equals(Purpose, other.Purpose) &&
                   string.Equals(Currency, other.Currency) && string.Equals(ContraAccountBankCode, other.ContraAccountBankCode) && string.Equals(ContraAccountNumber, other.ContraAccountNumber) &&
                   string.Equals(EndToEndId, other.EndToEndId) && string.Equals(MandateId, other.MandateId) && string.Equals(CreditorId, other.CreditorId) && string.Equals(Category, other.Category);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BookingDetailsBase) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (ContraAccountName != null ? ContraAccountName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (SubAccount != null ? SubAccount.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Purpose != null ? Purpose.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Currency != null ? Currency.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ContraAccountBankCode != null ? ContraAccountBankCode.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ContraAccountNumber != null ? ContraAccountNumber.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (EndToEndId != null ? EndToEndId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (MandateId != null ? MandateId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (CreditorId != null ? CreditorId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Category != null ? Category.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(BookingDetailsBase left, BookingDetailsBase right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BookingDetailsBase left, BookingDetailsBase right)
        {
            return !Equals(left, right);
        }
    }

    public class BookingDetails : BookingDetailsBase, IEquatable<BookingDetails>
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }

        public bool Equals(BookingDetails other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Date.Equals(other.Date) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BookingDetails) obj);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ Date.GetHashCode();
                hashCode = (hashCode*397) ^ Amount.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(BookingDetails left, BookingDetails right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BookingDetails left, BookingDetails right)
        {
            return !Equals(left, right);
        }
    }

    public class BookingDetailsOverride : BookingDetailsBase, IEquatable<BookingDetailsOverride>
    {
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }

        public bool Equals(BookingDetailsOverride other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Date.Equals(other.Date);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BookingDetailsOverride) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ Date.GetHashCode();
            }
        }

        public static bool operator ==(BookingDetailsOverride left, BookingDetailsOverride right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BookingDetailsOverride left, BookingDetailsOverride right)
        {
            return !Equals(left, right);
        }
    }
}