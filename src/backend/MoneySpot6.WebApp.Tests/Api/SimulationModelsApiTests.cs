using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.SimulationPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class SimulationModelsApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private async Task<DbSimulationModel> CreateTestModel(string name = "Test Simulation", string code = "// test")
    {
        var model = new DbSimulationModel { Name = name };
        Get<Db>().SimulationModels.Add(model);
        await Get<Db>().SaveChangesAsync();

        var revision = new DbSimulationModelRevision
        {
            SimulationModel = model,
            CreatedAt = DateTimeOffset.UtcNow,
            OriginalCode = code,
            CompiledCode = "",
            SourceMap = ""
        };
        Get<Db>().SimulationModelRevisions.Add(revision);
        await Get<Db>().SaveChangesAsync();

        return model;
    }

    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<SimulationModelsController>().GetAll();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAll_WithModels_ReturnsAll()
    {
        await CreateTestModel("Model 1");
        await CreateTestModel("Model 2");

        var result = await Get<SimulationModelsController>().GetAll();

        result.Length.ShouldBe(2);
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsNewModelId()
    {
        var result = await Get<SimulationModelsController>().Create(new NewSimulationModelRequest
        {
            Name = "Test Simulation",
            IncludeSampleCode = false
        });

        var modelId = result.ShouldBeOkObjectResult<int>();
        modelId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Create_WithSampleCode_CreatesModelWithCode()
    {
        var result = await Get<SimulationModelsController>().Create(new NewSimulationModelRequest
        {
            Name = "Test Simulation",
            IncludeSampleCode = true
        });

        var modelId = result.ShouldBeOkObjectResult<int>();
        var revision = Get<Db>().SimulationModelRevisions.Single(r => r.SimulationModel.Id == modelId);
        revision.OriginalCode.ShouldContain("onInit");
        revision.OriginalCode.ShouldContain("onTick");
    }

    [Test]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var result = await Get<SimulationModelsController>().Create(new NewSimulationModelRequest
        {
            Name = "",
            IncludeSampleCode = false
        });

        var error = result.ShouldBeBadRequestObjectResult<SimulationModelValidationErrorResponse>();
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task Create_DuplicateName_ReturnsBadRequest()
    {
        await CreateTestModel("Test Simulation");

        var result = await Get<SimulationModelsController>().Create(new NewSimulationModelRequest
        {
            Name = "Test Simulation",
            IncludeSampleCode = false
        });

        var error = result.ShouldBeBadRequestObjectResult<SimulationModelValidationErrorResponse>();
        error.NameAlreadyInUse.ShouldBeTrue();
    }

    [Test]
    public async Task GetById_ExistingModel_ReturnsModel()
    {
        var model = await CreateTestModel("Test Simulation", "// my code");

        var result = await Get<SimulationModelsController>().GetById(model.Id);

        var response = result.ShouldBeOkObjectResult<SimulationModelResponse>();
        response.Name.ShouldBe("Test Simulation");
        response.OriginalCode.ShouldBe("// my code");
        response.LatestRevisionId.ShouldNotBeNull();
    }

    [Test]
    public async Task GetById_NonExistingModel_ReturnsNotFound()
    {
        var result = await Get<SimulationModelsController>().GetById(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Update_CreatesNewRevision()
    {
        var model = await CreateTestModel();

        var result = await Get<SimulationModelsController>().Update(new UpdateSimulationModelRequest
        {
            Id = model.Id,
            OriginalCode = "// updated code",
            CompiledCode = "function() {}",
            SourceMap = "{}"
        });

        var revisionId = result.ShouldBeOkObjectResult<int>();
        revisionId.ShouldBeGreaterThan(0);
        Get<Db>().SimulationModelRevisions.Count(r => r.SimulationModel.Id == model.Id).ShouldBe(2);
    }

    [Test]
    public async Task Rename_ValidRequest_RenamesModel()
    {
        var model = await CreateTestModel("Original Name");

        var result = await Get<SimulationModelsController>().Rename(new RenameSimulationModelRequest
        {
            Id = model.Id,
            Name = "New Name"
        });

        result.ShouldBeOfType<OkResult>();
        Get<Db>().SimulationModels.Single().Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Rename_EmptyName_ReturnsBadRequest()
    {
        var model = await CreateTestModel();

        var result = await Get<SimulationModelsController>().Rename(new RenameSimulationModelRequest
        {
            Id = model.Id,
            Name = ""
        });

        var error = result.ShouldBeBadRequestObjectResult<SimulationModelValidationErrorResponse>();
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task Rename_DuplicateName_ReturnsBadRequest()
    {
        await CreateTestModel("Existing Name");
        var model = await CreateTestModel("To Rename");

        var result = await Get<SimulationModelsController>().Rename(new RenameSimulationModelRequest
        {
            Id = model.Id,
            Name = "Existing Name"
        });

        var error = result.ShouldBeBadRequestObjectResult<SimulationModelValidationErrorResponse>();
        error.NameAlreadyInUse.ShouldBeTrue();
    }

    [Test]
    public async Task Delete_ExistingModel_DeletesModel()
    {
        var model = await CreateTestModel();

        var result = await Get<SimulationModelsController>().Delete(model.Id);

        result.ShouldBeOfType<OkResult>();
        Get<Db>().SimulationModels.Count().ShouldBe(0);
    }

    [Test]
    public async Task Delete_NonExistingModel_ReturnsNotFound()
    {
        var result = await Get<SimulationModelsController>().Delete(999);

        result.ShouldBeOfType<NotFoundResult>();
    }
}


