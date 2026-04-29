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
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

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
        if (GetMode() == AuthMode.None)
            return Redirect("/");

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

        return Ok(new UserDetails(User.Identity.Name, GetMode()));
    }

    private AuthMode GetMode()
    {
        var raw = _configuration.GetValue<string>("Auth:Type");
        if (string.IsNullOrEmpty(raw) || raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            return AuthMode.None;
        return AuthMode.Oidc;
    }
}

[PublicAPI]
public enum AuthMode
{
    None = 0,
    Oidc = 1,
}

[PublicAPI]
public record UserDetails(string UserName, AuthMode Mode);
