using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MoneySpot6.WebApp.Features.AccountSync.Services;

[SingletonService]
public class SepaParser
{
    public ImmutableDictionary<Header, string> Parse(string[] lines)
    {
        var line = string.Join(LooksLikeSepa(lines) ? "" : " ", lines);
        var r = new Dictionary<Header, string?>();

        foreach (var header in Enum.GetValues<Header>())
        {
            r[header] = Extract(ref line, Remove(SepaStart(header.ToString()) + Content()) + SepaEnd());
        }

        var s = FixWhitespace(line);
        if (!string.IsNullOrWhiteSpace(s))
            if (!r.TryAdd(Header.SVWZ, s))
                r[Header.SVWZ] += s;

        return r.Where(x => x.Value != null).ToImmutableDictionary()!;
    }

    private string SepaStart(string name) => name + "(:|\\+)";
    private string SepaEnd() => "(?=((" + string.Join("|", Enum.GetNames<Header>()) + ")(:|\\+)|$))";
    private string Content() => "(?<content>.*?)";
    private string Remove(params string[] parts) => "(?<remove>" + string.Join("", parts) + ")";

    private string? Extract(ref string line, string pattern)
    {
        var list = new List<string>();

        while (true)
        {
            var match = Regex.Match(line, pattern);
            if (!match.Success)
                break;

            var remove = match.Groups["remove"];
            line = line.Remove(remove.Index, remove.Length);

            var content = FixWhitespace(match.Groups["content"].Value);
            if (content != null)
                list.Add(content);
        }
        
        list = list.Distinct().ToList();

        return list.Count == 0 ? null : string.Join(" / ", list);
    }

    private bool LooksLikeSepa(string[] lines)
    {
        var combines = string.Join("", lines);
        foreach (var name in Enum.GetNames<Header>())
        {
            if (combines.Contains(name + "+") || combines.Contains(name + ":"))
                return true;
        }
        return false;
    }

    private string? FixWhitespace(string line)
    {
        while (line.Contains("  "))
            line = line.Replace("  ", " ");
        line = line.Trim();
        return string.IsNullOrWhiteSpace(line) ? null : line;
    }
}

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
public enum Header
{
    EREF,
    KREF,
    MREF,
    CRED,
    DBET,
    SVWZ,
    ABWA,
    ABWE,
    IBAN,
    TAN1,
    PURP,
    ANAM,
    BIC
}