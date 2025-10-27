using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class CategoryConfigurationController : Controller
{
    private readonly Db _db;
    private readonly TransactionProcessingFacade _transactionProcessingFacade;

    public CategoryConfigurationController(Db db, TransactionProcessingFacade transactionProcessingFacade)
    {
        _db = db;
        _transactionProcessingFacade = transactionProcessingFacade;
    }

    [HttpGet("GetCategoryTree")]
    public async Task<ImmutableArray<CategoryResponse>> GetCategoryTree()
    {
        var all = await _db
            .Categories
            .ToDictionaryAsync(x => x.Id);
        
        ImmutableArray<CategoryResponse> GetChildren(int? parentId)
        {
            return [
                ..all.Values
                    .Where(x => x.ParentId == parentId)
                    .Select(x => new CategoryResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        AutoAssignmentCounterpartyRegex = x.AutoAssignmentCounterpartyRegex,
                        AutoAssignmentPurposeRegex = x.AutoAssignmentPurposeRegex,
                        Children = GetChildren(x.Id)
                    }).OrderBy(x => x.Name)
            ];
        }

        return GetChildren(null);
    }

    [HttpGet("GetCategoryPath")]
    public async Task<ImmutableArray<string>> GetCategoryPath(int id)
    {
        var all = await _db
            .Categories
            .ToDictionaryAsync(x => x.Id);

        var result = new List<string>();
        var currentNode = all[id];
        while (true)
        {
            result.Add(currentNode.Name);
            if (currentNode.ParentId == null)
                break;
            currentNode = all[currentNode.ParentId.Value];
        }

        result.Reverse();
        return [..result];
    }

    [HttpGet("GetCategory")]
    [ProducesResponseType<CategoryResponse>(200)]
    public async Task<IActionResult> GetCategory(int id)
    {
        var cat = await _db
            .Categories
            .SingleOrDefaultAsync(x => x.Id == id);

        if (cat == null)
            return NotFound();

        return Ok(cat);
    }

    [HttpPut("CreateCategory")]
    [ProducesResponseType<int>(200)]
    [ProducesResponseType<CreateCategoryValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new CreateCategoryValidationErrorResponse
            {
                MissingName = true
            });
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _db.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.ParentId.Value);

            if (parent == null)
            {
                return BadRequest(new CreateCategoryValidationErrorResponse
                {
                    InvalidParent = true
                });
            }
        }

        var neighbors = await _db.Categories
            .Where(x => x.ParentId == request.ParentId)
            .ToArrayAsync();

        CreateCategoryValidationErrorResponse? badRequestResponse = null;
        if (neighbors.Any(x => x.Name.Trim().Equals(request.Name.Trim(), StringComparison.InvariantCultureIgnoreCase)))
        {
            badRequestResponse ??= new CreateCategoryValidationErrorResponse();
            badRequestResponse.NameAlreadyInUse = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AutoAssignmentCounterpartyRegex) && !IsValidRegex(request.AutoAssignmentCounterpartyRegex))
        {
            badRequestResponse ??= new CreateCategoryValidationErrorResponse();
            badRequestResponse.InvalidAutoAssignmentCounterpartyRegex = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AutoAssignmentPurposeRegex) && !IsValidRegex(request.AutoAssignmentPurposeRegex))
        {
            badRequestResponse ??= new CreateCategoryValidationErrorResponse();
            badRequestResponse.InvalidAutoAssignmentPurposeRegex = true;
        }

        if (badRequestResponse != null)
            return BadRequest(badRequestResponse);

        var newCategory = new DbCategory
        {
            Name = request.Name,
            AutoAssignmentCounterpartyRegex = request.AutoAssignmentCounterpartyRegex,
            AutoAssignmentPurposeRegex = request.AutoAssignmentPurposeRegex,
            ParentId = request.ParentId
        };

        _db.Add(newCategory);
        await _db.SaveChangesAsync();

        return Ok(newCategory.Id);
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = Regex.Match("", pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }
        return true;
    }

    [HttpPost("Update")]
    [ProducesResponseType<UpdateCategoryValidationErrorResponse>(400)]
    public async Task<IActionResult> Update(UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new UpdateCategoryValidationErrorResponse
            {
                MissingName = true
            });
        }

        var cat = await _db
            .Categories
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == request.Id);

        if (cat == null)
            return NotFound();

        var neighbors = await _db.Categories
            .Where(x => x.ParentId == cat.ParentId)
            .ToArrayAsync();

        UpdateCategoryValidationErrorResponse? badRequestResponse = null;
        if (neighbors.Any(x => x.Name.Trim().Equals(request.Name.Trim(), StringComparison.InvariantCultureIgnoreCase) && x.Id != request.Id))
        {
            badRequestResponse ??= new UpdateCategoryValidationErrorResponse();
            badRequestResponse.NameAlreadyInUse = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AutoAssignmentCounterpartyRegex) && !IsValidRegex(request.AutoAssignmentCounterpartyRegex))
        {
            badRequestResponse ??= new UpdateCategoryValidationErrorResponse();
            badRequestResponse.InvalidAutoAssignmentCounterpartyRegex = true;
        }

        if (!string.IsNullOrWhiteSpace(request.AutoAssignmentPurposeRegex) && !IsValidRegex(request.AutoAssignmentPurposeRegex))
        {
            badRequestResponse ??= new UpdateCategoryValidationErrorResponse();
            badRequestResponse.InvalidAutoAssignmentPurposeRegex = true;
        }

        if (badRequestResponse != null)
            return BadRequest(badRequestResponse);

        cat.Name = request.Name;
        cat.AutoAssignmentCounterpartyRegex = request.AutoAssignmentCounterpartyRegex;
        cat.AutoAssignmentPurposeRegex = request.AutoAssignmentPurposeRegex;
        
        await _db.SaveChangesAsync();
        await _transactionProcessingFacade.UpdateTransactions();

        return Ok();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var all = await _db
            .Categories
            .ToDictionaryAsync(x => x.Id);

        void Del(DbCategory cat)
        {
            foreach (var child in all.Values.Where(x => x.ParentId == cat.Id)) 
                Del(child);
            _db.Categories.Remove(cat);
        }
        Del(all[id]);

        await _db.SaveChangesAsync();
        return Ok();
    }
}

[PublicAPI]
public record CreateCategoryRequest(string Name, string AutoAssignmentCounterpartyRegex, string AutoAssignmentPurposeRegex, int? ParentId);

[PublicAPI]
public record UpdateCategoryRequest(int Id, string Name, string AutoAssignmentCounterpartyRegex, string AutoAssignmentPurposeRegex);

[PublicAPI]
public record CategoryResponse
{
    [Required] public int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string AutoAssignmentCounterpartyRegex { get; init; }
    [Required] public required string AutoAssignmentPurposeRegex { get; init; }
    [Required] public required ImmutableArray<CategoryResponse> Children { get; init; }
}

[PublicAPI]
public record CreateCategoryValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
    [Required] public bool InvalidParent { get; set; }
    [Required] public bool InvalidAutoAssignmentCounterpartyRegex { get; set; }
    [Required] public bool InvalidAutoAssignmentPurposeRegex { get; set; }
}

[PublicAPI]
public record UpdateCategoryValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
    [Required] public bool InvalidAutoAssignmentCounterpartyRegex { get; set; }
    [Required] public bool InvalidAutoAssignmentPurposeRegex { get; set; }
}