using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public class CategoryUiTests : UiTest
{
    [Test]
    public async Task Shows_empty_state_when_no_categories()
    {
        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("categories-empty-state")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Keine Kategorien vorhanden")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_category_in_table()
    {
        var category = await CreateCategory(name: "Test Kategorie");

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"category-row-{category.Id}");
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Test Kategorie")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_open_create_dialog()
    {
        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-category-button").ClickAsync();

        await Expect(Page.GetByTestId("category-name-input")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_create_category()
    {
        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-category-button").ClickAsync();
        await Page.GetByTestId("category-name-input").FillAsync("Neue Test Kategorie");
        await Page.GetByTestId("category-submit-button").ClickAsync();

        // Wait for dialog to close
        await Expect(Page.GetByTestId("category-name-input")).Not.ToBeVisibleAsync();

        // Verify category appears in table
        await Expect(Page.GetByText("Neue Test Kategorie")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_create_dialog()
    {
        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-category-button").ClickAsync();
        await Expect(Page.GetByTestId("category-name-input")).ToBeVisibleAsync();

        await Page.GetByTestId("category-cancel-button").ClickAsync();

        await Expect(Page.GetByTestId("category-name-input")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_delete_category()
    {
        var category = await CreateCategory(name: "Category To Delete");

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"category-row-{category.Id}");
        await Expect(row).ToBeVisibleAsync();

        await row.GetByTestId("delete-category-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

        await Expect(row).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_delete_category()
    {
        var category = await CreateCategory(name: "Category To Keep");

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"category-row-{category.Id}");
        await row.GetByTestId("delete-category-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Nein" }).ClickAsync();

        await Expect(row).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_category()
    {
        var category = await CreateCategory(name: "Original Category Name");

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"category-row-{category.Id}");
        await row.GetByTestId("edit-category-button").ClickAsync();

        await Page.GetByTestId("category-name-input").FillAsync("Updated Category Name");
        await Page.GetByTestId("category-submit-button").ClickAsync();

        // Wait for dialog to close
        await Expect(Page.GetByTestId("category-name-input")).Not.ToBeVisibleAsync();

        // Verify updated name
        await Expect(Page.GetByText("Updated Category Name")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Name_field_is_required()
    {
        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-category-button").ClickAsync();
        await Page.GetByTestId("category-name-input").FillAsync("Dummy");
        await Page.GetByTestId("category-name-input").FillAsync("");
        await Page.GetByTestId("category-name-input").BlurAsync();

        await Expect(Page.GetByText("Ein Name muss angegeben werden.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_create_subcategory()
    {
        var parentCategory = await CreateCategory(name: "Parent Category");

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"category-row-{parentCategory.Id}");
        await row.GetByTestId("add-subcategory-button").ClickAsync();

        await Page.GetByTestId("category-name-input").FillAsync("Child Category");
        await Page.GetByTestId("category-submit-button").ClickAsync();

        // Wait for dialog to close
        await Expect(Page.GetByTestId("category-name-input")).Not.ToBeVisibleAsync();

        // Expand parent to see child
        await row.Locator("p-treetable-toggler button").ClickAsync();

        // Verify subcategory appears
        await Expect(Page.GetByText("Child Category")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_subcategory_in_tree()
    {
        var parentCategory = await CreateCategory(name: "Parent");
        var childCategory = await CreateCategory(name: "Child", parentId: parentCategory.Id);

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Parent should be visible
        var parentRow = Page.GetByTestId($"category-row-{parentCategory.Id}");
        await Expect(parentRow).ToBeVisibleAsync();

        // Expand parent to see child
        await parentRow.Locator("p-treetable-toggler button").ClickAsync();

        // Child should now be visible
        var childRow = Page.GetByTestId($"category-row-{childCategory.Id}");
        await Expect(childRow).ToBeVisibleAsync();
        await Expect(childRow.GetByText("Child")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Deleting_parent_deletes_children()
    {
        var parentCategory = await CreateCategory(name: "Parent To Delete");
        var childCategory = await CreateCategory(name: "Child To Delete", parentId: parentCategory.Id);

        await Page.GotoAsync("http://localhost:4200/settings/categories");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var parentRow = Page.GetByTestId($"category-row-{parentCategory.Id}");
        await parentRow.GetByTestId("delete-category-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

        // Both parent and child should be gone
        await Expect(parentRow).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId($"category-row-{childCategory.Id}")).Not.ToBeVisibleAsync();
    }

    private async Task<DbCategory> CreateCategory(string name = "Test Category", int? parentId = null)
    {
        var category = new DbCategory
        {
            Name = name,
            ParentId = parentId
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }
}


