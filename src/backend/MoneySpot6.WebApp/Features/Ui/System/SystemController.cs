using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.SelfUpdate;

namespace MoneySpot6.WebApp.Features.Ui.System;

[ApiController]
[Route("api/[controller]")]
public class SystemController : Controller
{
    private readonly Db _db;
    private readonly SelfUpdateFacade _selfUpdateFacade;

    public SystemController(Db db, SelfUpdateFacade selfUpdateFacade)
    {
        _db = db;
        _selfUpdateFacade = selfUpdateFacade;
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
