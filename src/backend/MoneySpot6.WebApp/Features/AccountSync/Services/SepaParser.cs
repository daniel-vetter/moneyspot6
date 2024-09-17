using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MoneySpot6.WebApp.Features.AccountSync.Services;

[SingletonService]
public class SepaParser2
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

[SingletonService]
public class SepaParser
{
    public ImmutableDictionary<Header, string> Parse(string[] lines)
    {
        if (!LooksLikeSepa(lines))
        {
            var dictionary = new Dictionary<Header, string>();
            var combined = string.Join(" ", lines);
            if (!string.IsNullOrWhiteSpace(combined))
                dictionary[Header.SVWZ] = FixWhitespace(combined);
            return dictionary.ToImmutableDictionary();
        }

        var line = string.Join("", lines);
        var parts = Extract(line, '+');
        if (!parts.TryGetValue(Header.SVWZ, out var svwz))
            return parts.ToImmutableDictionary();

        foreach (var (subHeader, subValue) in Extract(svwz, ':'))
        {
            if (subHeader == Header.SVWZ && !string.IsNullOrWhiteSpace(subValue))
                parts[subHeader] = subValue.Trim();

            parts.TryAdd(subHeader, subValue);
        }

        if (parts.TryGetValue(Header.SVWZ, out svwz))
            parts[Header.SVWZ] = FixWhitespace(svwz);

        FixIban(parts);


        return parts.ToImmutableDictionary();
    }

    private string FixWhitespace(string line)
    {
        while (line.Contains("  "))
            line = line.Replace("  ", " ");
        return line;
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

    private void FixIban(Dictionary<Header, string> parts)
    {
        if (!parts.TryGetValue(Header.IBAN, out var iban) || iban.Length <= 21)
            return;

        var space = iban[21..].IndexOf(' ');
        if (space == -1)
            return;

        parts[Header.IBAN] = iban[..(21 + space)];
        //var remaining = iban[(21 + space)..].Trim();
    }

    private static Dictionary<Header, string> Extract(string line, char sep)
    {
        var headers = string.Join("|", Enum.GetNames<Header>().Select(x => $"{x}\\{sep}").ToArray());
        var pattern = $"""(?<header>{headers})(?<content>.*?)(?={headers}|$)""";
        var matched = Regex.Matches(line, pattern);

        var result = new Dictionary<Header, string>();
        foreach (var match in matched.OrderByDescending(x => x.Index))
        {
            line = line.Remove(match.Index, match.Length);
            var header = Enum.Parse<Header>(match.Groups["header"].Value[..^1]);
            var content = match.Groups["content"].Value.Trim();

            if (!string.IsNullOrWhiteSpace(content))
                result[header] = content.Trim();
        }

        if (!string.IsNullOrWhiteSpace(line))
        {
            result[Header.SVWZ] = line.Trim() + result.GetValueOrDefault(Header.SVWZ, "");
        }

        return result;
    }

    private static ImmutableArray<HeaderPosition> FindHeaders(string purpose, char separator)
    {
        var l = ImmutableArray.CreateBuilder<HeaderPosition>();
        for (var i = 0; i < purpose.Length; i++)
        {
            if (purpose[i] != separator)
                continue;

            foreach (var header in Enum.GetValues<Header>())
            {
                var headerName = header.ToString();
                var start = i - headerName.Length;
                if (start < 0)
                    continue;

                if (purpose.Substring(start, headerName.Length).Equals(headerName, StringComparison.InvariantCultureIgnoreCase))
                {
                    l.Add(new HeaderPosition(header, start, headerName.Length + 1));
                }
            }
        }
        return l.ToImmutableArray();
    }

    private Dictionary<Header, string> GetParts(string purpose, char separator, out string? preText, string remaining)
    {
        preText = null;
        var headers = FindHeaders(purpose, separator);
        var r = new Dictionary<Header, string>();
        if (headers.Length == 0)
            return r;

        if (headers[0].Position != 0)
        {
            preText = purpose[..headers[0].Position];
            if (string.IsNullOrWhiteSpace(preText))
                preText = null;
        }

        for (var i = 0; i < headers.Length; i++)
        {
            var headerPos = headers[i];
            var end = i + 1 < headers.Length ? headers[i + 1].Position : purpose.Length;
            var length = end - (headerPos.Position + headerPos.Length);
            var str = purpose.Substring(headerPos.Position + headerPos.Length, length);
            if (!string.IsNullOrWhiteSpace(str))
                r.Add(headerPos.Header, str.Trim());
        }

        return r;
    }
}

record struct HeaderPosition(Header Header, int Position, int Length);

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