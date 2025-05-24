using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class CategoryConfigurationController : Controller
{
    private readonly Db _db;

    public CategoryConfigurationController(Db db)
    {
        _db = db;
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
                        Usages = 0,
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

        if (neighbors.Any(x => x.Name.Trim().Equals(request.Name.Trim(), StringComparison.InvariantCultureIgnoreCase)))
        {
            return BadRequest(new CreateCategoryValidationErrorResponse
            {
                NameAlreadyInUse = true
            });
        }

        var newCategory = new DbCategory
        {
            Name = request.Name,
            ParentId = request.ParentId
        };

        _db.Add(newCategory);
        await _db.SaveChangesAsync();

        return Ok(newCategory.Id);
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

        if (neighbors.Any(x => x.Name.Trim().Equals(request.Name.Trim(), StringComparison.InvariantCultureIgnoreCase) && x.Id != request.Id))
        {
            return BadRequest(new UpdateCategoryValidationErrorResponse
            {
                NameAlreadyInUse = true
            });
        }

        cat.Name = request.Name;

        await _db.SaveChangesAsync();
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

public record CreateCategoryRequest(string Name, int? ParentId);
public record UpdateCategoryRequest(int Id, string Name);
public record CategoryResponse
{
    [Required] public int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required int Usages { get; init; }
    [Required] public required ImmutableArray<CategoryResponse> Children { get; init; }
}

public record CreateCategoryValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
    [Required] public bool InvalidParent { get; set; }
}

public record UpdateCategoryValidationErrorResponse
{
    [Required] public bool MissingName { get; set; }
    [Required] public bool NameAlreadyInUse { get; set; }
}