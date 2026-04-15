using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.SelfUpdate;

namespace MoneySpot6.WebApp.Features.Ui.DebugPage;

[ApiController]
[Route("api/[controller]")]
public class UpdateController : Controller
{
    private readonly SelfUpdateFacade _selfUpdateFacade;
    private readonly Db _db;

    public UpdateController(SelfUpdateFacade selfUpdateFacade, Db db)
    {
        _selfUpdateFacade = selfUpdateFacade;
        _db = db;
    }

    [HttpGet("GetStatus")]
    public SelfUpdateStatus GetStatus()
    {
        return _selfUpdateFacade.GetStatus();
    }

    [HttpPost("CheckNow")]
    public async Task CheckNow()
    {
        await _selfUpdateFacade.CheckNow();
    }

    [HttpPost("ApplyUpdate")]
    public async Task ApplyUpdate()
    {
        await _selfUpdateFacade.ApplyUpdate();
    }

    [HttpGet("GetLogs")]
    public async Task<ImmutableArray<UpdateLogEntry>> GetLogs()
    {
        return [..await _db.UpdateLogs
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UpdateLogEntry(x.Id, x.CreatedAt, x.Log))
            .ToListAsync()];
    }
}

public record UpdateLogEntry(int Id, DateTimeOffset CreatedAt, string Log);
