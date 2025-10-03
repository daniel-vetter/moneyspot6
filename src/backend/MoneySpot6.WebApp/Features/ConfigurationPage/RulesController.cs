using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.ConfigurationPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesController : Controller
    {
        private readonly Db _db;

        public RulesController(Db db)
        {
            _db = db;
        }

        [HttpGet("GetAll")]
        public async Task<ImmutableArray<RuleResponse>> GetAll()
        {
            var rules = await _db.Rules.OrderBy(x => x.SortIndex).ToArrayAsync();
            return rules.Select(x => new RuleResponse
            {
                Id = x.Id,
                Name = x.Name,
                Script = x.Script
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
                Script = rule.Script
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
                Script = request.Script,
                SortIndex = maxSortKey + 1
            });

            await _db.SaveChangesAsync();
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
            existingRule.Script = request.Script;

            await _db.SaveChangesAsync();
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
            foreach(var id in request.Ids)
                if (!allRules.ContainsKey(id))
                    return BadRequest();

            for (int i = 0; i < request.Ids.Length; i++)
                allRules[request.Ids[i]].SortIndex = i + 1;

            await _db.SaveChangesAsync();
            return Ok();
        }


        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == id);
            if (rule == null)
                return NotFound();

            _db.Rules.Remove(rule);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public record RuleResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Script { get; set; }
    }

    public record CreateRuleRequest
    {
        [Required] public required string Name { get; set; }
        [Required] public required string Script { get; set; }
    }

    public record UpdateRuleRequest
    {
        [Required] public required int Id { get; set; }
        [Required] public required string Name { get; set; }
        [Required] public required string Script { get; set; }
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
}
