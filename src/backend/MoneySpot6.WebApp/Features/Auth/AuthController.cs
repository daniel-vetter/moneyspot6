using System.Security.Claims;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoneySpot6.WebApp.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public AuthController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string credential)
    {
        var jwt = await GoogleJsonWebSignature.ValidateAsync(credential);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, jwt.Subject),
            new(ClaimTypes.Name, jwt.Name),
            new(ClaimTypes.GivenName, jwt.GivenName),
            new(ClaimTypes.Surname, jwt.FamilyName),
            new(ClaimTypes.Name, jwt.Name),
            new(ClaimTypes.Email, jwt.Email)
        };

        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
            throw new Exception("No http context available");
        
        await ctx.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return new RedirectResult("/");
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
            throw new Exception("No http context available");

        await ctx.SignOutAsync();
        return new RedirectResult("/");
    }

    [HttpGet("GetCurrent")]
    [AllowAnonymous]
    [ProducesResponseType<GetCurrentResponse>(StatusCodes.Status200OK)]
    public IActionResult GetCurrent()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
            throw new Exception("No http context available");
        
        if (ctx.User.Identity == null || ctx.User.Identity.IsAuthenticated == false)
            return new OkObjectResult(new GetCurrentResponse(null));

        var sid = ctx.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid)?.Value ?? throw new Exception("No sid claim found");
        var name = ctx.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? throw new Exception("No name claim found");
        var mail = ctx.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? throw new Exception("No email claim found");

        return new OkObjectResult(new GetCurrentResponse(new UserResponse(sid, name, mail)));
    }
}
public record GetCurrentResponse(UserResponse? User);
public record UserResponse(
    string Sid,
    string Name,
    string Email
);