using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using Shouldly;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Tests.Api;

public class BankConnectionApiTests(string dbProvider) : ApiTest(dbProvider)
{
    private static CreateFinTsBankConnectionRequest ValidRequest => new()
    {
        Name = "Test Bank",
        HbciVersion = "300",
        BankCode = "12345678",
        CustomerId = "customer123",
        UserId = "user123",
        Pin = "secret123"
    };

    private DbBankConnection CreateTestConnection(string name = "Test Bank") => new()
    {
        Name = name,
        Type = BankConnectionType.FinTS,
        Settings = JsonSerializer.Serialize(new BankConnectionSettingsFinTS
        {
            HbciVersion = "300",
            BankCode = "12345678",
            CustomerId = "customer123",
            UserId = "user123",
            Pin = "secret123"
        })
    };

    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<BankConnectionController>().GetAll();

        result.ShouldBeOkObjectResult<ImmutableArray<BankConnectionListResponse>>().ShouldBeEmpty();
    }

    [Test]
    public async Task GetAll_WithConnections_ReturnsAll()
    {
        Get<Db>().BankConnections.Add(CreateTestConnection("Bank 1"));
        Get<Db>().BankConnections.Add(CreateTestConnection("Bank 2"));
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().GetAll();

        var connections = result.ShouldBeOkObjectResult<ImmutableArray<BankConnectionListResponse>>();
        connections.Length.ShouldBe(2);
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsNewConnectionId()
    {
        var result = await Get<BankConnectionController>().CreateFinTsConnection(ValidRequest);

        var connectionId = result.ShouldBeOkObjectResult<int>();
        connectionId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().CreateFinTsConnection(ValidRequest with { Name = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task Create_MissingHbciVersion_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().CreateFinTsConnection(ValidRequest with { HbciVersion = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingHbciVersion.ShouldBeTrue();
    }

    [Test]
    public async Task Create_MissingBankCode_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().CreateFinTsConnection(ValidRequest with { BankCode = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingBankCode.ShouldBeTrue();
    }

    [Test]
    public async Task Create_DuplicateName_ReturnsBadRequest()
    {
        Get<Db>().BankConnections.Add(CreateTestConnection());
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().CreateFinTsConnection(ValidRequest);

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.NameAlreadyExists.ShouldBeTrue();
    }

    [Test]
    public async Task Get_ExistingConnection_ReturnsConnectionDetails()
    {
        var connection = CreateTestConnection();
        Get<Db>().BankConnections.Add(connection);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().GetFinTsConnection(connection.Id);

        var response = result.ShouldBeOkObjectResult<BankConnectionDetailsResponse>();
        response.Name.ShouldBe("Test Bank");
        response.HbciVersion.ShouldBe("300");
        response.BankCode.ShouldBe("12345678");
    }

    [Test]
    public async Task Get_NonExistingConnection_ReturnsNotFound()
    {
        var result = await Get<BankConnectionController>().GetFinTsConnection(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Update_ValidRequest_UpdatesConnection()
    {
        var connection = CreateTestConnection();
        Get<Db>().BankConnections.Add(connection);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().UpdateFinTsConnection(new UpdateFinTsBankConnectionRequest
        {
            Id = connection.Id,
            Name = "Updated Bank",
            HbciVersion = "400",
            BankCode = "87654321",
            CustomerId = "newcustomer",
            UserId = "newuser",
            Pin = "newpin"
        });

        result.ShouldBeOfType<OkResult>();

        var updatedConnection = Get<Db>().BankConnections.Single();
        updatedConnection.Name.ShouldBe("Updated Bank");

        var settings = JsonSerializer.Deserialize<BankConnectionSettingsFinTS>(updatedConnection.Settings)!;
        settings.BankCode.ShouldBe("87654321");
        settings.HbciVersion.ShouldBe("400");
    }

    [Test]
    public async Task Update_NonExistingConnection_ReturnsNotFound()
    {
        var result = await Get<BankConnectionController>().UpdateFinTsConnection(new UpdateFinTsBankConnectionRequest
        {
            Id = 999,
            Name = "Test",
            HbciVersion = "300",
            BankCode = "12345678",
            CustomerId = "customer",
            UserId = "user",
            Pin = "pin"
        });

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Delete_ExistingConnection_DeletesConnection()
    {
        var connection = CreateTestConnection();
        Get<Db>().BankConnections.Add(connection);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().Delete(connection.Id);

        result.ShouldBeOfType<OkResult>();
        Get<Db>().BankConnections.Count().ShouldBe(0);
    }

    [Test]
    public async Task Delete_NonExistingConnection_ReturnsNotFound()
    {
        var result = await Get<BankConnectionController>().Delete(999);

        result.ShouldBeOfType<NotFoundResult>();
    }
}


