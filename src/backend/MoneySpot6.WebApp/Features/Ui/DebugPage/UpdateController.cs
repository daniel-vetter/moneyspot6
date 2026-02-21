using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Features.Core.SelfUpdate;

namespace MoneySpot6.WebApp.Features.Ui.DebugPage;

[ApiController]
[Route("api/[controller]")]
public class UpdateController : Controller
{
    private readonly SelfUpdateFacade _selfUpdateFacade;

    public UpdateController(SelfUpdateFacade selfUpdateFacade)
    {
        _selfUpdateFacade = selfUpdateFacade;
    }

    [HttpGet("GetStatus")]
    public SelfUpdateStatus GetStatus()
    {
        return _selfUpdateFacade.GetStatus();
    }

    [HttpPost("CheckNow")]
    public async Task CheckNow(CancellationToken cancellationToken)
    {
        await _selfUpdateFacade.CheckNow(cancellationToken);
    }

    [HttpPost("ApplyUpdate")]
    public async Task ApplyUpdate(CancellationToken cancellationToken)
    {
        await _selfUpdateFacade.ApplyUpdate(cancellationToken);
    }
}
