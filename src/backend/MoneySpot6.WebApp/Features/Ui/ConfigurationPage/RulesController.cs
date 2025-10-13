using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing.RuleSystem;
using NJsonSchema;
using NJsonSchema.Converters;
using NJsonSchema.Generation;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class RulesController : Controller
{
    private readonly Db _db;
    private readonly TransactionProcessor _transactionProcessor;
    private readonly RuleCategoryKeyProvider _ruleCategoryKeyProvider;

    public RulesController(Db db, TransactionProcessor transactionProcessor, RuleCategoryKeyProvider ruleCategoryKeyProvider)
    {
        _db = db;
        _transactionProcessor = transactionProcessor;
        _ruleCategoryKeyProvider = ruleCategoryKeyProvider;
    }

    [HttpGet("GetAll")]
    public async Task<ImmutableArray<RuleResponse>> GetAll()
    {
        var rules = await _db.Rules.OrderBy(x => x.SortIndex).ToArrayAsync();
        return rules.Select(x => new RuleResponse
        {
            Id = x.Id,
            Name = x.Name,
            OriginalCode = x.OriginalCode
        }).ToImmutableArray();
    }

    [HttpGet("GetById")]
    [Produces<RuleResponse>]
    [ProducesResponseType<RuleValidationErrorResponse>(400)]
    public async Task<IActionResult> GetById(int id)
    {
        var rule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == id);
        if (rule == null)
            return NotFound();

        return Ok(new RuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            OriginalCode = rule.OriginalCode
        });
    }

    [HttpPut("Create")]
    [ProducesResponseType<RuleValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(CreateRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new RuleValidationErrorResponse
            {
                MissingName = true
            });

        if (await _db.Rules.AnyAsync(x => x.Name == request.Name))
            return BadRequest(new RuleValidationErrorResponse
            {
                NameAlreadyInUse = true
            });

        var maxSortKey = await _db.Rules.MaxAsync(x => (int?)x.SortIndex) ?? 0;

        _db.Rules.Add(new DbRule
        {
            Name = request.Name,
            OriginalCode = request.OriginalCode,
            CompiledCode = request.CompiledCode,
            SourceMap = request.SourceMap,
            SortIndex = maxSortKey + 1
        });

        await _db.SaveChangesAsync();
        await _transactionProcessor.UpdateAll();
        return Ok();
    }

    [HttpPost("Update")]
    public async Task<IActionResult> Update(UpdateRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new RuleValidationErrorResponse
            {
                MissingName = true
            });

        if (await _db.Rules.AnyAsync(x => x.Name == request.Name && x.Id != request.Id))
            return BadRequest(new RuleValidationErrorResponse
            {
                NameAlreadyInUse = true
            });

        var existingRule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == request.Id);
        if (existingRule == null)
            return NotFound();

        existingRule.Name = request.Name;
        existingRule.OriginalCode = request.OriginalCode;
        existingRule.CompiledCode = request.CompiledCode;
        existingRule.SourceMap = request.SourceMap;

        await _db.SaveChangesAsync();
        await _transactionProcessor.UpdateAll();
        return Ok();
    }

    [HttpPost("Reorder")]
    public async Task<IActionResult> Reorder(ReorderRulesRequest request)
    {
        //Check for dublicates
        if (request.Ids.Distinct().Count() != request.Ids.Length)
            return BadRequest();

        var allRules = await _db.Rules.ToDictionaryAsync(x => x.Id, x => x);

        //Check for requested ids that do not exist
        foreach (var id in request.Ids)
            if (!allRules.ContainsKey(id))
                return BadRequest();

        for (int i = 0; i < request.Ids.Length; i++)
            allRules[request.Ids[i]].SortIndex = i + 1;
        
        await _db.SaveChangesAsync();
        await _transactionProcessor.UpdateAll();
        return Ok();
    }

    [HttpGet("CategoryKeys")]
    public async Task<ImmutableArray<CateogryKeyResponse>> GetCategoryKeys()
    {
        return (await _ruleCategoryKeyProvider.GetAll()).Select(x => new CateogryKeyResponse
        {
            Id = x.Id,
            Name = x.Name
        }).ToImmutableArray();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == id);
        if (rule == null)
            return NotFound();

        _db.Rules.Remove(rule);
        await _db.SaveChangesAsync();
        await _transactionProcessor.UpdateAll();
        return Ok();
    }
}

public record RuleResponse
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
}

public record CreateRuleRequest
{
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

public record UpdateRuleRequest
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

public record RuleValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
}

public record ReorderRulesRequest
{
    [Required] public ImmutableArray<int> Ids { get; set; }
}

public record CateogryKeyResponse
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
}