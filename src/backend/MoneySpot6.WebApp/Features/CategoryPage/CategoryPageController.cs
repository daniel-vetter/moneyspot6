using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using NJsonSchema;
using NJsonSchema.Annotations;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.CategoryPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryPageController : Controller
    {
        private readonly Db _db;

        public CategoryPageController(Db db)
        {
            _db = db;
        }

        [HttpGet("GetSankeyData")]
        [ProducesResponseType<SankeyDataResponse>(200)]
        public async Task<IActionResult> GetSankeyData(
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly start,
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly end)
        {
            var categories = new Categories(await _db
                .Categories
                .AsNoTracking()
                .Select(x => new Category(x.Id, x.Name, x.ParentId, false))
                .ToArrayAsync());

            var transactions = await _db.BankAccountTransactions
                .AsNoTracking()
                .Where(x => x.Parsed.Date >= start && x.Parsed.Date < end)
                .Select(x => new
                {
                    x.Final.Amount,
                    x.Final.CategoryId
                })
                .ToArrayAsync();

            var conBuilder = new ConnectionBuilder(categories);
            foreach (var transaction in transactions)
            {
                var fixedCategory = categories.GetFixed(transaction.CategoryId);
                var isIncome = transaction.Amount >= 0;
                conBuilder.Add(fixedCategory, isIncome, isIncome ? transaction.Amount : -transaction.Amount);
            }

            var connections = conBuilder
                .GetConnections()
                .Select(x => new ConnectionResponse
                {
                    From = x.From.Id,
                    To = x.To.Id,
                    Amount = x.Value
                })
                .ToImmutableArray();

            var nodes = conBuilder
                .GetNodes().Select(x => new NodeResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Column = x.Column
                })
                .ToImmutableArray();

            return Ok(new SankeyDataResponse
            {
                Nodes = nodes,
                Connections = connections
            });
        }
    }

    public record SankeyDataResponse
    {
        public required ImmutableArray<NodeResponse> Nodes { get; init; }
        public required ImmutableArray<ConnectionResponse> Connections { get; init; }
    }
    public record NodeResponse
    {
        [Required] public required string Id { get; init; }
        [Required] public required string Name { get; init; }
        [Required] public required int Column { get; init; }
    }
    public record ConnectionResponse
    {
        [Required] public required string From { get; init; }
        [Required] public required string To { get; init; }
        [Required] public required decimal Amount { get; init; }
    }
}


public record Category(int Id, string Name, int? ParentId, bool IsFake);


public class Categories
{
    private readonly Dictionary<int, Category> _list;
    private readonly Dictionary<ParentId, Category> _others = new();
    private int _lastId;
    private record struct ParentId(int? Id);

    public Categories(IEnumerable<Category> initial)
    {
        _list = initial.ToDictionary(x => x.Id);
        _lastId = _list.Count == 0 ? 0 : _list.Keys.Max();
    }

    public int GetFixed(int? id)
    {
        int CreateOrGetOther(int? parentId)
        {
            if (_others.TryGetValue(new ParentId(parentId), out var existing))
                return existing.Id;

            _lastId++;
            var newCat = new Category(_lastId, "Sonstiges", parentId, true);
            _others[new ParentId(parentId)] = newCat;
            _list[_lastId] = newCat;
            return newCat.Id;
        }

        if (id == null)
            return CreateOrGetOther(null);

        if (_list.Any(x => x.Value.ParentId == id))
            return CreateOrGetOther(id.Value);

        return id.Value;
    }

    public Category GetById(int id)
    {
        return _list[id];
    }

    public int GetDepth(int id)
    {
        var count = 0;
        while (true)
        {
            count++;
            var next = _list[id];
            if (!next.ParentId.HasValue)
                return count;
            id = next.ParentId!.Value;
        }
    }

    public IEnumerable<Category> GetPath(int category)
    {
        var r = new List<Category>();
        var cur = _list[category];
        while (true)
        {
            r.Add(cur);
            if (!cur.ParentId.HasValue)
                break;
            cur = _list[cur.ParentId.Value];
        }
        r.Reverse();
        return r.ToImmutableArray();
    }
}

public class ConnectionBuilder
{
    private readonly Categories _categories;
    private readonly Dictionary<string, Node> _nodes = new();
    private readonly List<Connection> _connections = new();


    public ConnectionBuilder(Categories categories)
    {
        _categories = categories;
    }

    public Connection[] GetConnections()
    {
        return _connections
            .GroupBy(x => (x.From, x.To))
            .Select(x => new Connection(x.Key.From, x.Key.To, x.Select(y => y.Value).Sum()))
            .OrderBy(x => x.From.Column)
            .ThenBy(x => x.From.Id)
            .OrderBy(x => x.To.Column)
            .ThenBy(x => x.To.Id)
            .ToArray();
    }

    public void Add(int target, bool isIncome, decimal value)
    {
        foreach (var (from, to) in GetSegmentsToRoot(target))
        {
            var start = GetNode(from, isIncome);
            var end = GetNode(to, isIncome);


            if (isIncome)
                _connections.Add(new Connection(start, end, value));
            else
                _connections.Add(new Connection(end, start, value));
        }

    }

    private Node GetNode(int? categoryId, bool isIncome)
    {
        var id = categoryId.HasValue 
            ? (isIncome ? "in>" : "out>") + string.Join(">", _categories.GetPath(categoryId.Value).Select(x => $"{x.Name}@{x.Id}").ToArray()) 
            : "root";

        if (_nodes.TryGetValue(id, out var existing))
            return existing;

        var newNode = new Node(
            Id: id,
            Name: categoryId == null ? "Budget" : _categories.GetById(categoryId.Value).Name,
            IsIncome: categoryId != null && isIncome,
            Column: categoryId == null ? 0 : _categories.GetDepth(categoryId.Value) * (isIncome ? -1 : 1)
        );

        _nodes[id] = newNode;
        return newNode;
    }

    public (int From, int? To)[] GetSegmentsToRoot(int start)
    {
        var r = new List<(int From, int? To)>();
        var cur = start;
        while (true)
        {
            var next = _categories.GetById(cur).ParentId;
            r.Add((cur, next));
            if (!next.HasValue)
                break;
            cur = next.Value;
        }
        return r.ToArray();
    }

    public Node[] GetNodes()
    {
        if (_nodes.Count == 0)
            return [];

        var offset = 0 - _nodes.Values.Select(x => x.Column).Min();
        return _nodes.Values
            .Select(x => new Node(x.Id, x.Name, x.IsIncome, x.Column + offset))
            .OrderBy(x => x.Id)
            .ToArray();
    }
}

public record Node(string Id, string Name, bool IsIncome, int Column)
{
}

public record Connection(Node From, Node To, decimal Value);