using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing.Internal;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class RulesController : Controller
{
    private readonly RuleCategoryKeyProvider _ruleCategoryKeyProvider;
    private readonly TransactionProcessingFacade _ruleSystemFacade;

    public RulesController(RuleCategoryKeyProvider ruleCategoryKeyProvider, TransactionProcessingFacade ruleSystemFacade)
    {
        _ruleCategoryKeyProvider = ruleCategoryKeyProvider;
        _ruleSystemFacade = ruleSystemFacade;
    }

    [HttpGet("GetAll")]
    [Produces<RuleResponse[]>]
    public async Task<ImmutableArray<RuleResponse>> GetAll()
    {
        return (await _ruleSystemFacade.GetAllRules()).Select(x => new RuleResponse
        {
            Id = x.Id,
            Name = x.Name,
            OriginalCode = x.OriginalCode,
            HasSyntaxErrors = x.HasSyntaxErrors
        }).ToImmutableArray();
    }

    [HttpGet("GetById")]
    [Produces<RuleResponse>]
    [ProducesResponseType<RuleValidationErrorResponse>(400)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _ruleSystemFacade.GetRuleById(id);

        return result.Match<IActionResult>(
            r => Ok(new RuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                OriginalCode = r.OriginalCode,
                HasSyntaxErrors = r.HasSyntaxErrors
            }),
            e => e.RuleIdNotFound 
                ? NotFound() 
                : throw new Exception("Unknown error")
        );
    }

    [HttpPut("Create")]
    [Produces<int>]
    [ProducesResponseType<RuleValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(NewRuleRequest request)
    {
        var result = await _ruleSystemFacade.CreateRule(new NewRule
        {
            Name = request.Name,
            OriginalCode = request.OriginalCode,
            CompiledCode = request.CompiledCode,
            SourceMap = request.SourceMap,
        });

        return result.Match<IActionResult>(
            s => Ok(s),
            e => BadRequest(new RuleValidationErrorResponse
            {
                MissingName = e.MissingName,
                NameAlreadyInUse = e.NameAlreadyInUse
            }));
    }

    [HttpPost("Update")]
    [ProducesResponseType<RuleValidationErrorResponse>(400)]
    public async Task<IActionResult> Update(UpdateRuleRequest request)
    {
        var result = await _ruleSystemFacade.UpdateRule(new UpdateRule
        {
            Id = request.Id,
            Name = request.Name,
            OriginalCode = request.OriginalCode,
            CompiledCode = request.CompiledCode,
            SourceMap = request.SourceMap,
        });

        return result.Match<IActionResult>(
            Ok,
            e => e.RuleIdNotFound
                ? NotFound()
                : BadRequest(new RuleValidationErrorResponse
                {
                    MissingName = e.MissingName,
                    NameAlreadyInUse = e.NameAlreadyInUse
                })
        );
    }

    [HttpPost("Reorder")]
    public async Task<IActionResult> Reorder(ReorderRulesRequest request)
    {
        var result = await _ruleSystemFacade.ReorderRules(request.Ids);

        return result.Match<IActionResult>(
            Ok,
            _ => BadRequest()
        );
    }

    [HttpGet("CategoryKeys")]
    public async Task<ImmutableArray<CategoryKeyResponse>> GetCategoryKeys()
    {
        return (await _ruleCategoryKeyProvider.GetAll()).Select(x => new CategoryKeyResponse
        {
            Id = x.Id,
            Name = x.Name
        }).ToImmutableArray();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _ruleSystemFacade.Delete(id);
        return result.Match<IActionResult>(
            Ok,
            e => e.RuleIdNotFound 
                ? NotFound() 
                : throw new Exception("Unknown error")
        );
    }
}

[PublicAPI]
public record RuleResponse
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public bool HasSyntaxErrors { get; set; }
}

[PublicAPI]
public record NewRuleRequest
{
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

[PublicAPI]
public record UpdateRuleRequest
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string OriginalCode { get; set; }
    [Required] public required string CompiledCode { get; set; }
    [Required] public required string SourceMap { get; set; }
}

[PublicAPI]
public record RuleValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
}

[PublicAPI]
public record ReorderRulesRequest
{
    [Required] public ImmutableArray<int> Ids { get; set; }
}

[PublicAPI]
public record CategoryKeyResponse
{
    [Required] public required int Id { get; set; }
    [Required] public required string Name { get; set; }
}