using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.Inflation;
using MoneySpot6.WebApp.Features.Ui.InflationData;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class InflationDataApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    [Test]
    public async Task UpdateDefaultRate_SetsRate()
    {
        var result = await Get<InflationDataController>().UpdateDefaultRate(new UpdateDefaultRateRequest
        {
            DefaultRate = 2.5m
        });

        result.ShouldBeOfType<OkResult>();
        (await Get<IConfigService>().Get<decimal>(InflationCalculator.DefaultRateConfigKey)).ShouldBe(2.5m);
    }

    [Test]
    public async Task GetAll_WithDefaultRate_ReturnsData()
    {
        await Get<IConfigService>().Set(InflationCalculator.DefaultRateConfigKey, 2.0m);

        var result = await Get<InflationDataController>().GetAll(projectionYears: 1);

        var data = result.ShouldBeOkObjectResult<InflationDataResponse>();
        data.Entries.ShouldNotBeEmpty();
        data.DefaultRate.ShouldBe(2.0m);
    }
}
