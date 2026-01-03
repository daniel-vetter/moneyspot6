using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.InflationData;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class InflationDataApiTests : ApiTest
{
    [Test]
    public async Task UpdateDefaultRate_SetsRate()
    {
        var result = await Get<InflationDataController>().UpdateDefaultRate(new UpdateDefaultRateRequest
        {
            DefaultRate = 2.5m
        });

        result.ShouldBeOfType<OkResult>();
        Get<Db>().InflationSettings.Single().DefaultRate.ShouldBe(2.5m);
    }

    [Test]
    public async Task GetAll_WithDefaultRate_ReturnsData()
    {
        Get<Db>().InflationSettings.Add(new DbInflationSettings { DefaultRate = 2.0m });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<InflationDataController>().GetAll(projectionYears: 1);

        var data = result.ShouldBeOkObjectResult<InflationDataResponse>();
        data.Entries.ShouldNotBeEmpty();
        data.DefaultRate.ShouldBe(2.0m);
    }
}
