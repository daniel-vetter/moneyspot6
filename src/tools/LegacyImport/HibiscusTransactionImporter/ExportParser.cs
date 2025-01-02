using System.Collections.Immutable;
using System.Globalization;
using System.Xml;

namespace HibiscusTransactionImporter;

public class ExportParser
{
    public static ImmutableArray<HibiscusTransaction> Parse(string path)
    {
        var r = ImmutableArray.CreateBuilder<HibiscusTransaction>();
        var xml = new XmlDocument();
        xml.LoadXml(File.ReadAllText(path));
        foreach (XmlElement node in xml.ChildNodes.OfType<XmlElement>().Single().ChildNodes)
        {
            var ht = new HibiscusTransaction();
            ht.Datum = DateTime.Parse(Required(node, "datum"));
            ht.Valuta = DateTime.Parse(Required(node, "valuta"));
            ht.EmpfaengerKonto = Optional(node, "empfaenger_konto");
            ht.Primanota = OptionalInt(node, "primanota");
            ht.EmpfaengerName = Optional(node, "empfaenger_name");
            ht.CustomerRef = Required(node, "customerref");
            ht.Checksum = Required(node, "checksum");
            ht.Zweck = Required(node, "zweck");
            ht.CreditorId = Optional(node, "creditorid");
            ht.PurposeCode = Optional(node, "purposecode");
            ht.Art = Required(node, "art");
            ht.Betrag = RequiredDouble(node, "betrag");
            ht.KontoId = RequiredInt(node, "konto_id");
            ht.TxId = Optional(node, "txid");
            ht.Saldo = RequiredDouble(node, "saldo");
            ht.EndToEndId = Optional(node, "endtoendid");
            ht.MandateId = Optional(node, "mandateid");
            ht.EmpfaengerBlz = Optional(node, "empfaenger_blz");
            r.Add(ht);
        }

        return r.ToImmutable().Reverse().ToImmutableArray();
    }

    private static string? Optional(XmlElement element, string name)
    {
        var value = element[name]?.InnerText;

        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static int RequiredInt(XmlElement element, string name)
    {
        var str = Required(element, name);
        return int.Parse(str);
    }

    private static int? OptionalInt(XmlElement element, string name)
    {
        var str = Optional(element, name);
        if (str == null)
            return null;

        return int.Parse(str);
    }

    private static double RequiredDouble(XmlElement element, string name)
    {
        var str = Required(element, name);

        return double.Parse(str, CultureInfo.InvariantCulture);
    }

    private static string Required(XmlElement element, string name)
    {
        var value = Optional(element, name);
        if (value == null)
            throw new Exception("Missing: " + name);

        return value;
    }
}



public record HibiscusTransaction
{
    public DateTime Datum { get; set; }
    public DateTime Valuta { get; set; }
    public string? EmpfaengerKonto { get; set; }
    public string? EmpfaengerName { get; set; }
    public int? Primanota { get; set; }
    public string CustomerRef { get; set; }
    public string Checksum { get; set; }
    public string Zweck { get; set; }
    public string? CreditorId { get; set; }
    public string? PurposeCode { get; set; }
    public string Art { get; set; }
    public double? Betrag { get; set; }
    public int KontoId { get; set; }
    public string? TxId { get; set; }
    public double Saldo { get; set; }
    public string? EndToEndId { get; set; }
    public string? MandateId { get; set; }
    public string? EmpfaengerBlz { get; set; }
}