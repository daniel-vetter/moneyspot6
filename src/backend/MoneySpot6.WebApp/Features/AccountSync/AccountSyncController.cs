using Microsoft.AspNetCore.Mvc;

namespace MoneySpot6.WebApp.Features.AccountSync;

[ApiController]
[Route("[controller]")]
public class AccountSyncController : Controller
{
    [HttpGet]
    public ActionResult<string> Get()
    {
        return "Hello World";
    }
}