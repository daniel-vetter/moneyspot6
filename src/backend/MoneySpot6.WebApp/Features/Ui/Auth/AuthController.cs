using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoneySpot6.WebApp.Features.Ui.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : Controller
{
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string returnUri = "/")
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUri,
        }, "oidc");
    }

    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "oidc", CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("UserDetails")]
    [AllowAnonymous]
    [ProducesResponseType<UserDetails>(200)]
    public IActionResult GetUserDetails()
    {
        if (User.Identity?.Name == null)
            return Ok(null);

        return Ok(new UserDetails(User.Identity.Name));
    }
}

[PublicAPI]
public record UserDetails(string UserName);