using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public class RuleUiTests : UiTest
{
    [Test]
    public async Task Shows_empty_state_when_no_rules()
    {
        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("rules-empty-state")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Keine Regeln vorhanden")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_rule_in_table()
    {
        var rule = await CreateRule(name: "Test Regel");

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Test Regel")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_open_create_dialog()
    {
        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-rule-button").ClickAsync();

        await Expect(Page.GetByTestId("rule-name-input")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_create_rule()
    {
        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-rule-button").ClickAsync();
        await Page.GetByTestId("rule-name-input").FillAsync("Neue Test Regel");

        // Wait for Monaco to be ready and code validation to pass
        await Expect(Page.GetByText("Keine Fehler gefunden.")).ToBeVisibleAsync();

        await Page.GetByTestId("rule-submit-button").ClickAsync();

        // Wait for dialog to close
        await Expect(Page.GetByTestId("rule-name-input")).Not.ToBeVisibleAsync();

        // Verify rule appears in table
        await Expect(Page.GetByText("Neue Test Regel")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_create_dialog()
    {
        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-rule-button").ClickAsync();
        await Expect(Page.GetByTestId("rule-name-input")).ToBeVisibleAsync();

        await Page.GetByTestId("rule-cancel-button").ClickAsync();

        await Expect(Page.GetByTestId("rule-name-input")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_delete_rule()
    {
        var rule = await CreateRule(name: "Rule To Delete");

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await Expect(row).ToBeVisibleAsync();

        await row.GetByTestId("delete-rule-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

        await Expect(row).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_delete_rule()
    {
        var rule = await CreateRule(name: "Rule To Keep");

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await row.GetByTestId("delete-rule-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Nein" }).ClickAsync();

        await Expect(row).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_rule()
    {
        var rule = await CreateRule(name: "Original Rule Name");

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await row.GetByTestId("edit-rule-button").ClickAsync();

        await Page.GetByTestId("rule-name-input").FillAsync("Updated Rule Name");

        // Wait for code validation
        await Expect(Page.GetByText("Keine Fehler gefunden.")).ToBeVisibleAsync();

        await Page.GetByTestId("rule-submit-button").ClickAsync();

        // Wait for dialog to close
        await Expect(Page.GetByTestId("rule-name-input")).Not.ToBeVisibleAsync();

        // Verify updated name
        await Expect(Page.GetByText("Updated Rule Name")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Name_field_is_required()
    {
        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-rule-button").ClickAsync();
        await Page.GetByTestId("rule-name-input").FillAsync("Dummy");
        await Page.GetByTestId("rule-name-input").FillAsync("");
        await Page.GetByTestId("rule-name-input").BlurAsync();

        await Expect(Page.GetByText("Ein Name muss angegeben werden.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_syntax_error_tag()
    {
        var rule = await CreateRule(name: "Rule With Error", hasSyntaxIssues: true);

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await Expect(row.GetByText("Syntax Fehler")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_runtime_error_tag()
    {
        var rule = await CreateRule(name: "Rule With Runtime Error", runtimeError: "Some error occurred");

        await Page.GotoAsync("http://localhost:4200/settings/rules");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"rule-row-{rule.Id}");
        await Expect(row.GetByText("Laufzeit Fehler")).ToBeVisibleAsync();
    }

    private async Task<DbRule> CreateRule(
        string name = "Test Rule",
        string originalCode = "export function run(t: Transaction) { }",
        string compiledCode = "export function run(t) { }",
        string sourceMap = "{}",
        bool hasSyntaxIssues = false,
        string? runtimeError = null)
    {
        var maxSortIndex = _db.Rules.Any() ? _db.Rules.Max(r => r.SortIndex) : 0;
        var rule = new DbRule
        {
            Name = name,
            OriginalCode = originalCode,
            CompiledCode = compiledCode,
            SourceMap = sourceMap,
            SortIndex = maxSortIndex + 1,
            HasSyntaxIssues = hasSyntaxIssues,
            RuntimeError = runtimeError
        };
        _db.Rules.Add(rule);
        await _db.SaveChangesAsync();
        return rule;
    }
}


