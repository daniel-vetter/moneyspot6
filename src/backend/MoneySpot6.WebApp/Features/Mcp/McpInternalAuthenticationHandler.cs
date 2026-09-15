using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Features.Ui.Auth;

namespace MoneySpot6.WebApp.Features.Mcp;

/// <summary>
/// Per-process secret that the in-process MCP self-call uses to authenticate against the controllers.
/// Generated once at startup; never leaves the process.
/// </summary>
public sealed class McpInternalSecret
{
    public string Value { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}

/// <summary>
/// Authenticates the MCP bridge's in-process loopback calls. A normal request never carries the secret
/// header, so this scheme only ever authenticates the bridge itself, acting as the singleton admin user.
/// Only wired up when OIDC is active — under the default NoAuth mode every request is already the admin user.
/// </summary>
public sealed class McpInternalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "McpInternal";
    public const string SecretHeaderName = "X-MoneySpot-Mcp-Secret";

    private readonly McpInternalSecret _secret;

    public McpInternalAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        McpInternalSecret secret) : base(options, logger, encoder)
    {
        _secret = secret;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SecretHeaderName, out var provided))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(_secret.Value);
        if (!CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
            return Task.FromResult(AuthenticateResult.Fail("Invalid MCP internal secret."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, NoAuthAuthenticationHandler.SingletonUserId),
            new Claim(ClaimTypes.Name, NoAuthAuthenticationHandler.SingletonUserName),
            new Claim(ClaimTypes.Role, NoAuthAuthenticationHandler.AdminRole),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
