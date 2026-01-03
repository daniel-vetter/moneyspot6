using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using Shouldly;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Tests.Api;

public class BankConnectionApiTests : ApiTest
{
    private static CreateBankConnectionRequest ValidRequest => new()
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
        HbciVersion = "300",
        BankCode = "12345678",
        CustomerId = "customer123",
        UserId = "user123",
        Pin = "secret123"
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
        var result = await Get<BankConnectionController>().Create(ValidRequest);

        var connectionId = result.ShouldBeOkObjectResult<int>();
        connectionId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().Create(ValidRequest with { Name = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingName.ShouldBeTrue();
    }

    [Test]
    public async Task Create_MissingHbciVersion_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().Create(ValidRequest with { HbciVersion = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingHbciVersion.ShouldBeTrue();
    }

    [Test]
    public async Task Create_MissingBankCode_ReturnsBadRequest()
    {
        var result = await Get<BankConnectionController>().Create(ValidRequest with { BankCode = "" });

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.MissingBankCode.ShouldBeTrue();
    }

    [Test]
    public async Task Create_DuplicateName_ReturnsBadRequest()
    {
        Get<Db>().BankConnections.Add(CreateTestConnection());
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().Create(ValidRequest);

        var error = result.ShouldBeBadRequestObjectResult<BankConnectionValidationErrorResponse>();
        error.NameAlreadyExists.ShouldBeTrue();
    }

    [Test]
    public async Task Get_ExistingConnection_ReturnsConnectionDetails()
    {
        var connection = CreateTestConnection();
        Get<Db>().BankConnections.Add(connection);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().Get(connection.Id);

        var response = result.ShouldBeOkObjectResult<BankConnectionDetailsResponse>();
        response.Name.ShouldBe("Test Bank");
        response.HbciVersion.ShouldBe("300");
        response.BankCode.ShouldBe("12345678");
    }

    [Test]
    public async Task Get_NonExistingConnection_ReturnsNotFound()
    {
        var result = await Get<BankConnectionController>().Get(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Update_ValidRequest_UpdatesConnection()
    {
        var connection = CreateTestConnection();
        Get<Db>().BankConnections.Add(connection);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<BankConnectionController>().Update(new UpdateBankConnectionRequest
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
        Get<Db>().BankConnections.Single().Name.ShouldBe("Updated Bank");
        Get<Db>().BankConnections.Single().BankCode.ShouldBe("87654321");
    }

    [Test]
    public async Task Update_NonExistingConnection_ReturnsNotFound()
    {
        var result = await Get<BankConnectionController>().Update(new UpdateBankConnectionRequest
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
