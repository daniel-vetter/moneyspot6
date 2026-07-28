using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Gate;
using MoneySpot6.WebApp.Features.Core.SelfUpdate;

namespace MoneySpot6.WebApp.Features.Ui.System;

[ApiController]
[Route("api/[controller]")]
public class SystemController : Controller
{
    private readonly Db _db;
    private readonly SelfUpdateFacade _selfUpdateFacade;
    private readonly GateService _gateService;

    public SystemController(Db db, SelfUpdateFacade selfUpdateFacade, GateService gateService)
    {
        _db = db;
        _selfUpdateFacade = selfUpdateFacade;
        _gateService = gateService;
    }

    [HttpGet("GetAppDetails")]
    public AppDetails GetAppDetails()
    {
        var databaseType = _db switch
        {
            PostgreSqlDbContext => "PostgreSQL",
            SqliteDbContext => "SQLite",
            _ => "Unknown"
        };

        return new AppDetails(
            Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown",
            Environment.GetEnvironmentVariable("BUILD_COMMIT") ?? "unknown",
            Environment.Version.ToString(),
            RuntimeInformation.OSDescription,
            databaseType
        );
    }

    [HttpGet("GetUpdateStatus")]
    public async Task<SelfUpdateStatus> GetUpdateStatus()
    {
        return await _selfUpdateFacade.GetStatus();
    }

    [HttpPost("CheckForUpdate")]
    public async Task CheckForUpdate()
    {
        await _selfUpdateFacade.CheckNow();
    }

    [HttpPost("ApplyUpdate")]
    public async Task ApplyUpdate()
    {
        await _selfUpdateFacade.ApplyUpdate();
    }

    [HttpPost("SetAutoUpdate")]
    public async Task SetAutoUpdate(SetAutoUpdateRequest request)
    {
        await _selfUpdateFacade.SetAutoUpdate(request.Enabled);
    }

    [HttpGet("GetUpdateLogs")]
    public async Task<ImmutableArray<UpdateLogEntry>> GetUpdateLogs()
    {
        return [..await _db.UpdateLogs
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UpdateLogEntry(x.Id, x.CreatedAt, x.Log))
            .ToListAsync()];
    }

    [HttpGet("GetGateConfig")]
    public async Task<GateConfigResponse> GetGateConfig()
    {
        var config = await _gateService.GetConfig();
        return new GateConfigResponse(config.Url, config.Enabled);
    }

    [HttpPost("SetGateConfig")]
    public async Task SetGateConfig(SetGateConfigRequest request)
    {
        await _gateService.SetConfig(request.Url, request.Enabled);
    }
}

[PublicAPI]
public record AppDetails(string BuildTime, string BuildCommit, string DotNetVersion, string OSDescription, string DatabaseType);

[PublicAPI]
public record UpdateLogEntry(int Id, DateTimeOffset CreatedAt, string Log);

[PublicAPI]
public record SetAutoUpdateRequest
{
    [Required] public required bool Enabled { get; init; }
}

[PublicAPI]
public record GateConfigResponse([property: Required] string Url, [property: Required] bool Enabled);

[PublicAPI]
public record SetGateConfigRequest
{
    [Required] public required string Url { get; init; }
    [Required] public required bool Enabled { get; init; }
}
