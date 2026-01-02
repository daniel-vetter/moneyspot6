using Microsoft.AspNetCore.Mvc;
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
    public async Task CreateCategory_ValidRequest_ReturnsNewCategoryId()
    {
        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", null));

        result.ShouldBeOfType<OkObjectResult>();
        var categoryId = (int)((OkObjectResult)result).Value!;
        categoryId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task CreateCategory_WithParent_CreatesChildCategory()
    {
        var parentResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Food", null));
        var parentId = (int)((OkObjectResult)parentResult).Value!;

        var childResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", parentId));

        childResult.ShouldBeOfType<OkObjectResult>();
        var tree = await Get<CategoryConfigurationController>().GetCategoryTree();
        tree.Length.ShouldBe(1);
        tree[0].Name.ShouldBe("Food");
        tree[0].Children.Length.ShouldBe(1);
        tree[0].Children[0].Name.ShouldBe("Groceries");
    }

    [Test]
    public async Task CreateCategory_EmptyName_ReturnsBadRequest()
    {
        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("", null));

        result.ShouldBeOfType<BadRequestObjectResult>();
        var error = (CreateCategoryValidationErrorResponse)((BadRequestObjectResult)result).Value!;
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task CreateCategory_DuplicateName_ReturnsBadRequest()
    {
        await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", null));

        var result = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", null));

        result.ShouldBeOfType<BadRequestObjectResult>();
        var error = (CreateCategoryValidationErrorResponse)((BadRequestObjectResult)result).Value!;
        error.NameAlreadyInUse.ShouldBeTrue();
    }

    [Test]
    public async Task GetCategoryPath_ReturnsFullPath()
    {
        var foodResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Food", null));
        var foodId = (int)((OkObjectResult)foodResult).Value!;

        var groceriesResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", foodId));
        var groceriesId = (int)((OkObjectResult)groceriesResult).Value!;

        var vegetablesResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Vegetables", groceriesId));
        var vegetablesId = (int)((OkObjectResult)vegetablesResult).Value!;

        var path = await Get<CategoryConfigurationController>().GetCategoryPath(vegetablesId);

        path.ShouldBe(["Food", "Groceries", "Vegetables"]);
    }

    [Test]
    public async Task DeleteCategory_DeletesWithChildren()
    {
        var parentResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Food", null));
        var parentId = (int)((OkObjectResult)parentResult).Value!;
        await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Groceries", parentId));

        var deleteResult = await Get<CategoryConfigurationController>().Delete(parentId);

        deleteResult.ShouldBeOfType<OkResult>();
        var tree = await Get<CategoryConfigurationController>().GetCategoryTree();
        tree.ShouldBeEmpty();
    }

    [Test]
    public async Task UpdateCategory_ChangesName()
    {
        var createResult = await Get<CategoryConfigurationController>().Create(new CreateCategoryRequest("Food", null));
        var categoryId = (int)((OkObjectResult)createResult).Value!;

        var updateResult = await Get<CategoryConfigurationController>().Update(new UpdateCategoryRequest(categoryId, "Lebensmittel"));

        updateResult.ShouldBeOfType<OkResult>();
        var tree = await Get<CategoryConfigurationController>().GetCategoryTree();
        tree[0].Name.ShouldBe("Lebensmittel");
    }
}
