using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class CategoryConfigurationApiTests : ApiTest
{
    [Test]
    public async Task GetCategoryTree_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<CategoryConfigurationController>().GetCategoryTree();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetCategoryTree_WithCategories_ReturnsTree()
    {
        var food = new DbCategory { Name = "Food" };
        var groceries = new DbCategory { Name = "Groceries", ParentId = 1 };
        Get<Db>().Categories.Add(food);
        await Get<Db>().SaveChangesAsync();
        groceries.ParentId = food.Id;
        Get<Db>().Categories.Add(groceries);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<CategoryConfigurationController>().GetCategoryTree();

        result.Length.ShouldBe(1);
        result[0].Name.ShouldBe("Food");
        result[0].Children.Length.ShouldBe(1);
        result[0].Children[0].Name.ShouldBe("Groceries");
    }

    [Test]
    public async Task CreateCategory_ValidRequest_ReturnsNewCategoryId()
    {
        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", null));

        var categoryId = result.ShouldBeOkObjectResult<int>();
        categoryId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task CreateCategory_WithParent_CreatesChildCategory()
    {
        Get<Db>().Categories.Add(new DbCategory { Name = "Food" });
        await Get<Db>().SaveChangesAsync();
        var parentId = Get<Db>().Categories.Single().Id;

        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", parentId));

        result.ShouldBeOfType<OkObjectResult>();
        var tree = await Get<CategoryConfigurationController>().GetCategoryTree();
        tree[0].Children.Length.ShouldBe(1);
        tree[0].Children[0].Name.ShouldBe("Groceries");
    }

    [Test]
    public async Task CreateCategory_EmptyName_ReturnsBadRequest()
    {
        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("", null));

        var error = result.ShouldBeBadRequestObjectResult<CreateCategoryValidationErrorResponse>();
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task CreateCategory_DuplicateName_ReturnsBadRequest()
    {
        Get<Db>().Categories.Add(new DbCategory { Name = "Groceries" });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", null));

        var error = result.ShouldBeBadRequestObjectResult<CreateCategoryValidationErrorResponse>();
        error.NameAlreadyInUse.ShouldBeTrue();
    }

    [Test]
    public async Task GetCategoryPath_ReturnsFullPath()
    {
        var food = new DbCategory { Name = "Food" };
        Get<Db>().Categories.Add(food);
        await Get<Db>().SaveChangesAsync();
        var groceries = new DbCategory { Name = "Groceries", ParentId = food.Id };
        Get<Db>().Categories.Add(groceries);
        await Get<Db>().SaveChangesAsync();
        var vegetables = new DbCategory { Name = "Vegetables", ParentId = groceries.Id };
        Get<Db>().Categories.Add(vegetables);
        await Get<Db>().SaveChangesAsync();

        var path = await Get<CategoryConfigurationController>().GetCategoryPath(vegetables.Id);

        path.ShouldBe(["Food", "Groceries", "Vegetables"]);
    }

    [Test]
    public async Task DeleteCategory_DeletesWithChildren()
    {
        var food = new DbCategory { Name = "Food" };
        Get<Db>().Categories.Add(food);
        await Get<Db>().SaveChangesAsync();
        Get<Db>().Categories.Add(new DbCategory { Name = "Groceries", ParentId = food.Id });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<CategoryConfigurationController>().Delete(food.Id);

        result.ShouldBeOfType<OkResult>();
        Get<Db>().Categories.Count().ShouldBe(0);
    }

    [Test]
    public async Task UpdateCategory_ChangesName()
    {
        var category = new DbCategory { Name = "Food" };
        Get<Db>().Categories.Add(category);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<CategoryConfigurationController>().Update(new UpdateCategoryRequest(category.Id, "Lebensmittel"));

        result.ShouldBeOfType<OkResult>();
        Get<Db>().Categories.Single().Name.ShouldBe("Lebensmittel");
    }
}
