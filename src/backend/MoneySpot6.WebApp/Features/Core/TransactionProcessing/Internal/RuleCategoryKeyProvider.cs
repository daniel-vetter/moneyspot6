using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using System.Collections.Immutable;
using System.Text;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing.Internal;

[ScopedService]
public class RuleCategoryKeyProvider
{
    private readonly Db _db;

    public RuleCategoryKeyProvider(Db db)
    {
        _db = db;
    }

    public async Task<ImmutableArray<CategoryKey>> GetAll()
    {
        var all = await _db.Categories.ToDictionaryAsync(x => x.Id, x => x);
        var allNames = all.ToDictionary(x => x.Key, x => GetFixedName(x.Value.Name));
        var sb = new StringBuilder();

        void Append(int id)
        {
            var c = all[id];
            if (c.ParentId.HasValue)
                Append(c.ParentId.Value);
            if (sb.Length != 0)
                sb.Append('_');
            sb.Append(allNames[id]);
        }

        var keys = ImmutableArray.CreateBuilder<CategoryKey>(all.Count);
        foreach (var cat in all)
        {
            sb.Clear();
            Append(cat.Value.Id);
            keys.Add(new CategoryKey(cat.Value.Id, sb.ToString()));
        }

        return keys.ToImmutable();
    }

    private string GetFixedName(string name)
    {
        name = name.Trim();
        bool isLetter(char c) =>
            c >= 'a' && c <= 'z' ||
            c >= 'A' && c <= 'Z' || 
            c == '_';

        bool isNumber(char c) =>
            c >= '0' && c <= '9';

        if (name.Length == 0)
            return "_";

        var sb = new StringBuilder();
        for (int i=0;i<name.Length;i++)
        {
            if (i == 0 && !isLetter(name[0]))
                sb.Append('_');

            if (isLetter(name[i]) || isNumber(name[i]))
                sb.Append(name[i]);
            else
                sb.Append('_');
        }
        return sb.ToString();
    }
}

public record class CategoryKey(int Id, string Name);
