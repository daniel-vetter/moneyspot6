using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MoneySpot6.WebApp.Features.Ui.Auth;

public class NoAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "NoAuth";
    public const string SingletonUserId = "default";
    public const string AdminRole = "admin";

    private readonly IConfiguration _configuration;

    public NoAuthAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var displayName = _configuration.GetValue<string>("Auth:NoAuthDisplayName") ?? "User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, SingletonUserId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Role, AdminRole),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
