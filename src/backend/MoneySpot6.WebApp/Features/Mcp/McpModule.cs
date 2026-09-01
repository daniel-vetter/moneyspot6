using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace MoneySpot6.WebApp.Features.Mcp;

/// <summary>
/// Wires up the MCP server: a curated set of controller actions (those marked with <see cref="McpToolAttribute"/>)
/// is exposed as MCP tools via <see cref="McpApiBridge"/>, which calls the real endpoints in-process.
/// The <c>/mcp</c> endpoint is protected as an OAuth 2.0 resource server (MCP authorization spec) — see
/// <see cref="AddMcpAuthentication"/>.
/// </summary>
public static class McpModule
{
    /// <summary>Named <see cref="HttpClient"/> the bridge uses for in-process loopback calls to our own controllers.</summary>
    public const string SelfHttpClientName = "mcp-self";

    /// <summary>Composite authenticate scheme used only when OIDC is active (see <c>Program</c>).</summary>
    public const string SmartAuthenticationScheme = "MoneySpotSmartAuth";

    /// <summary>Marker registered once MCP OAuth is configured; drives whether <see cref="MapMoneySpotMcp"/> exposes the endpoint.</summary>
    internal sealed class McpAuthenticationMarker;

    public static IServiceCollection AddMoneySpotMcp(this IServiceCollection services)
    {
        services.AddSingleton<McpInternalSecret>();
        services.AddSingleton<McpApiBridge>();

        services.AddHttpClient(SelfHttpClientName)
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = ResolveSelfBaseAddress(sp);
                client.DefaultRequestHeaders.Add(
                    McpInternalAuthenticationHandler.SecretHeaderName,
                    sp.GetRequiredService<McpInternalSecret>().Value);
            });

        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithListToolsHandler((request, ct) =>
                request.Services!.GetRequiredService<McpApiBridge>().ListToolsAsync(request, ct))
            .WithCallToolHandler((request, ct) =>
                request.Services!.GetRequiredService<McpApiBridge>().CallToolAsync(request, ct));

        return services;
    }

    /// <summary>
    /// Makes the app an OAuth 2.0 resource server for MCP. A client hitting <c>/mcp</c> without a token gets a
    /// 401 pointing at the protected-resource metadata, discovers the authorization server (your OIDC provider),
    /// runs the login flow and returns with a bearer JWT that <see cref="JwtBearerDefaults"/> validates.
    /// The MCP scheme forwards authentication to JwtBearer but keeps its own challenge (the metadata 401), so the
    /// UI's cookie/OIDC schemes stay untouched.
    /// </summary>
    public static AuthenticationBuilder AddMcpAuthentication(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        // The MCP client is its own Authentik application (public + PKCE), so its issuer differs from the UI's
        // Auth:Authority — point at it via Mcp:Authority (falls back to Auth:Authority if you run a shared issuer).
        // The MCP resource identifier is the public /mcp URL under Domain.
        // Fail soft: if either is missing, skip MCP auth rather than crash startup — MapMoneySpotMcp then leaves
        // /mcp unmapped.
        var authority = configuration.GetValue<string>("Mcp:Authority")
            ?? configuration.GetValue<string>("Auth:Authority");
        var domain = configuration.GetValue<string>("Domain");
        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(domain))
            return builder;

        var resource = domain.TrimEnd('/') + "/mcp";
        string[] scopes = ["openid", "profile", "email"]; // same scopes the UI's OIDC client requests

        builder.Services.AddSingleton<McpAuthenticationMarker>();

        builder
            .AddJwtBearer(options =>
            {
                // JwtBearer lazily fetches the OIDC metadata/JWKS from the authority on first token validation.
                // Validate issuer + signature; audience validation is intentionally off — for a small self-hosted
                // deployment, trusting any token from our own Authentik is an acceptable simplification.
                options.Authority = authority;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = false;
            })
            .AddMcp(options =>
            {
                options.ForwardAuthenticate = JwtBearerDefaults.AuthenticationScheme;

                var metadata = new ProtectedResourceMetadata { Resource = resource };
                metadata.AuthorizationServers.Add(authority);
                foreach (var scope in scopes)
                    metadata.ScopesSupported.Add(scope);
                options.ResourceMetadata = metadata;
            });

        return builder;
    }

    /// <summary>
    /// Maps the MCP endpoint behind the OAuth policy. If MCP authentication was not configured (e.g. NoAuth mode,
    /// where there is no authorization server), the endpoint is deliberately not exposed.
    /// </summary>
    public static IEndpointRouteBuilder MapMoneySpotMcp(this IEndpointRouteBuilder endpoints, string pattern = "/mcp")
    {
        var logger = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Mcp");

        if (endpoints.ServiceProvider.GetService<McpAuthenticationMarker>() is null)
        {
            logger.LogWarning(
                "MCP endpoint not mapped: OAuth is not configured. It is only exposed when OIDC is active " +
                "(Auth:Type=oidc) and Domain is set, so that a real authorization server backs the login.");
            return endpoints;
        }

        var policy = new AuthorizationPolicyBuilder(McpAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();

        endpoints.MapMcp(pattern).RequireAuthorization(policy);
        logger.LogInformation("MCP endpoint mapped at {Pattern} (OAuth protected).", pattern);
        return endpoints;
    }

    private static Uri ResolveSelfBaseAddress(IServiceProvider sp)
    {
        var addresses = sp.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
            ?? throw new InvalidOperationException("Server addresses feature is unavailable; cannot call controllers in-process.");

        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("No server address available for in-process MCP calls.");

        var builder = new UriBuilder(address);
        if (builder.Host is "0.0.0.0" or "::" or "[::]")
            builder.Host = "localhost";
        if (!builder.Path.EndsWith('/'))
            builder.Path += "/";

        return builder.Uri;
    }
}
