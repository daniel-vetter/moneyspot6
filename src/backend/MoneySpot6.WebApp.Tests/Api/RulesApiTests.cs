using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class RulesApiTests : ApiTest
{
    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<RulesController>().GetAll();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAll_WithRules_ReturnsAll()
    {
        Get<Db>().Rules.Add(new DbRule { Name = "Rule 1", OriginalCode = "// 1", CompiledCode = "", SourceMap = "", SortIndex = 1 });
        Get<Db>().Rules.Add(new DbRule { Name = "Rule 2", OriginalCode = "// 2", CompiledCode = "", SourceMap = "", SortIndex = 2 });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<RulesController>().GetAll();

        result.Length.ShouldBe(2);
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsNewRuleId()
    {
        var result = await Get<RulesController>().Create(new NewRuleRequest
        {
            Name = "Test Rule",
            OriginalCode = "// test",
            CompiledCode = "function() {}",
            SourceMap = "{}"
        });

        var ruleId = result.ShouldBeOkObjectResult<int>();
        ruleId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GetById_ExistingRule_ReturnsRule()
    {
        var rule = new DbRule { Name = "Test Rule", OriginalCode = "// test code", CompiledCode = "", SourceMap = "", SortIndex = 1 };
        Get<Db>().Rules.Add(rule);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<RulesController>().GetById(rule.Id);

        var response = result.ShouldBeOkObjectResult<RuleResponse>();
        response.Name.ShouldBe("Test Rule");
        response.OriginalCode.ShouldBe("// test code");
    }

    [Test]
    public async Task GetById_NonExistingRule_ReturnsNotFound()
    {
        var result = await Get<RulesController>().GetById(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Update_ValidRequest_UpdatesRule()
    {
        var rule = new DbRule { Name = "Original", OriginalCode = "// original", CompiledCode = "", SourceMap = "", SortIndex = 1 };
        Get<Db>().Rules.Add(rule);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<RulesController>().Update(new UpdateRuleRequest
        {
            Id = rule.Id,
            Name = "Updated",
            OriginalCode = "// updated",
            CompiledCode = "function() {}",
            SourceMap = "{}"
        });

        result.ShouldBeOfType<OkResult>();
        Get<Db>().Rules.Single().Name.ShouldBe("Updated");
        Get<Db>().Rules.Single().OriginalCode.ShouldBe("// updated");
    }

    [Test]
    public async Task Update_NonExistingRule_ReturnsNotFound()
    {
        var result = await Get<RulesController>().Update(new UpdateRuleRequest
        {
            Id = 999,
            Name = "Test",
            OriginalCode = "// test",
            CompiledCode = "function() {}",
            SourceMap = "{}"
        });

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Delete_ExistingRule_DeletesRule()
    {
        var rule = new DbRule { Name = "To Delete", OriginalCode = "", CompiledCode = "", SourceMap = "", SortIndex = 1 };
        Get<Db>().Rules.Add(rule);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<RulesController>().Delete(rule.Id);

        result.ShouldBeOfType<OkResult>();
        Get<Db>().Rules.Count().ShouldBe(0);
    }

    [Test]
    public async Task Delete_NonExistingRule_ReturnsNotFound()
    {
        var result = await Get<RulesController>().Delete(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task GetCategoryKeys_ReturnsCategories()
    {
        Get<Db>().Categories.Add(new DbCategory { Name = "Food" });
        Get<Db>().Categories.Add(new DbCategory { Name = "Transport" });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<RulesController>().GetCategoryKeys();

        result.Length.ShouldBe(2);
        result.ShouldContain(x => x.Name == "Food");
        result.ShouldContain(x => x.Name == "Transport");
    }
}
