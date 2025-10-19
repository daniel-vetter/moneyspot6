using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing.Internal;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing
{
    [ScopedService]
    public class TransactionProcessingFacade
    {
        private readonly Db _db;
        private readonly TransactionProcessor _transactionProcessor;

        public TransactionProcessingFacade(Db db, TransactionProcessor transactionProcessor)
        {
            _db = db;
            _transactionProcessor = transactionProcessor;
        }

        public async Task<ImmutableArray<Rule>> GetAllRules()
        {
            var rules = await _db.Rules.OrderBy(x => x.SortIndex).ToArrayAsync();
            return rules.Select(x => new Rule
            {
                Id = x.Id,
                Name = x.Name,
                OriginalCode = x.OriginalCode,
                HasSyntaxErrors = x.HasSyntaxIssues,
                RuntimeError = x.RuntimeError
            }).ToImmutableArray();
        }

        public async Task<Result<Rule, RuleByIdError>> GetRuleById(int id)
        {
            var rule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == id);
            if (rule == null)
                return Result<Rule, RuleByIdError>.Fail(new RuleByIdError
                {
                    RuleIdNotFound = true
                });

            return Result<Rule, RuleByIdError>.Ok(new Rule
            {
                Id = rule.Id,
                Name = rule.Name,
                OriginalCode = rule.OriginalCode,
                HasSyntaxErrors = rule.HasSyntaxIssues,
                RuntimeError = rule.RuntimeError
            });
        }

        public async Task<Result<RuleByIdError>> Delete(int id)
        {
            var rule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == id);
            if (rule == null)
                return Result<RuleByIdError>.Fail(new RuleByIdError
                {
                    RuleIdNotFound = true
                });

            _db.Rules.Remove(rule);
            await _db.SaveChangesAsync();
            await UpdateTransactions();
            return Result<RuleByIdError>.Ok();
        }

        public async Task<Result<int, RuleError>> CreateRule(NewRule newRule)
        {
            if (string.IsNullOrWhiteSpace(newRule.Name))
                return Result<int, RuleError>.Fail(new RuleError
                {
                    MissingName = true
                });

            if (await _db.Rules.AnyAsync(x => x.Name == newRule.Name))
                return Result<int, RuleError>.Fail(new RuleError
                {
                    NameAlreadyInUse = true
                });

            var maxSortKey = await _db.Rules.MaxAsync(x => (int?)x.SortIndex) ?? 0;
            var r = new DbRule
            {
                Name = newRule.Name,
                OriginalCode = newRule.OriginalCode,
                CompiledCode = newRule.CompiledCode,
                SourceMap = newRule.SourceMap,
                SortIndex = maxSortKey + 1
            };
            _db.Rules.Add(r);

            await _db.SaveChangesAsync();
            await UpdateTransactions();

            return Result<int, RuleError>.Ok(r.Id);
        }

        public async Task<Result<RuleError>> UpdateRule(UpdateRule updateRule)
        {
            if (string.IsNullOrWhiteSpace(updateRule.Name))
                return Result<RuleError>.Fail(new RuleError
                {
                    MissingName = true
                });

            if (await _db.Rules.AnyAsync(x => x.Name == updateRule.Name && x.Id != updateRule.Id))
                return Result<RuleError>.Fail(new RuleError
                {
                    NameAlreadyInUse = true
                });

            var existingRule = await _db.Rules.SingleOrDefaultAsync(x => x.Id == updateRule.Id);
            if (existingRule == null)
                return Result<RuleError>.Fail(new RuleError
                {
                    RuleIdNotFound = true
                });

            existingRule.Name = updateRule.Name;
            existingRule.OriginalCode = updateRule.OriginalCode;
            existingRule.CompiledCode = updateRule.CompiledCode;
            existingRule.SourceMap = updateRule.SourceMap;

            await _db.SaveChangesAsync();
            await UpdateTransactions();
            return Result<RuleError>.Ok();
        }

        public async Task<Result<RuleReorderError>> ReorderRules(ImmutableArray<int> orderedIds)
        {
            //Check for dublicates
            if (orderedIds.Distinct().Count() != orderedIds.Length)
                return Result<RuleReorderError>.Fail(new RuleReorderError());

            var allRules = await _db.Rules.ToDictionaryAsync(x => x.Id, x => x);

            //Check for requested ids that do not exist
            if (orderedIds.Any(id => !allRules.ContainsKey(id)))
                return Result<RuleReorderError>.Fail(new RuleReorderError());

            for (var i = 0; i < orderedIds.Length; i++)
                allRules[orderedIds[i]].SortIndex = i + 1;

            await _db.SaveChangesAsync();
            await UpdateTransactions();

            return Result<RuleReorderError>.Ok();
        }

        public async Task UpdateTransactions(ImmutableArray<int>? ids = null)
        {
            await _transactionProcessor.Update(ids);
        }
    }

    public record Rule
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string OriginalCode { get; init; }
        public required bool HasSyntaxErrors { get; init; }
        public required string? RuntimeError { get; set; }
    }

    public record NewRule
    {
        public required string Name { get; init; }
        public required string OriginalCode { get; init; }
        public required string CompiledCode { get; init; }
        public required string SourceMap { get; init; }
    }

    public record UpdateRule
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string OriginalCode { get; init; }
        public required string CompiledCode { get; init; }
        public required string SourceMap { get; init; }
    }

    public record RuleByIdError
    {
        public bool RuleIdNotFound { get; init; }
    }

    public record RuleError
    {
        public bool MissingName { get; init; }
        public bool NameAlreadyInUse { get; init; }
        public bool RuleIdNotFound { get; init; }
    }

    public record RuleReorderError
    {
    }
}