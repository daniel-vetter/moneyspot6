using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.Gate;
using MoneySpot6.WebApp.Features.Core.SampleData;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.AppState;

[ApiController]
[Route("api/[controller]")]
public class AppStateController : Controller
{
    public const string IsFirstSetupDoneConfigKey = "IsFirstSetupDone";

    private readonly KeyValueConfiguration _config;
    private readonly SampleDataSeeder _sampleDataSeeder;
    private readonly GateService _gateService;

    public AppStateController(KeyValueConfiguration config, SampleDataSeeder sampleDataSeeder, GateService gateService)
    {
        _config = config;
        _sampleDataSeeder = sampleDataSeeder;
        _gateService = gateService;
    }

    [HttpGet("Get")]
    [ProducesResponseType<AppState>(200)]
    public async Task<IActionResult> Get()
    {
        var isFirstSetupDone = await _config.Get(IsFirstSetupDoneConfigKey, false);
        var gate = await _gateService.Check();
        return Ok(new AppState(isFirstSetupDone, gate.Blocked, gate.Message));
    }

    [HttpPost("CompleteFirstSetup")]
    public async Task CompleteFirstSetup(CompleteFirstSetupRequest request)
    {
        if (request.AddSampleData)
            await _sampleDataSeeder.Seed();

        await _config.Set(IsFirstSetupDoneConfigKey, true);
    }
}

[PublicAPI]
public record AppState(
    [property: Required] bool IsFirstSetupDone,
    [property: Required] bool Blocked,
    string? BlockMessage);

[PublicAPI]
public record CompleteFirstSetupRequest
{
    [Required] public required bool AddSampleData { get; init; }
}
