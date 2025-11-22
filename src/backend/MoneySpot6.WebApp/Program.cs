using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoneySpot6.ServiceDefaults;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.AccountSync.Adapter;
using MoneySpot6.WebApp.Features.Ui.AccountSync;
using MoneySpot6.WebApp.Infrastructure;
using NJsonSchema.Generation;
using Microsoft.AspNetCore.HttpOverrides;

namespace MoneySpot6.WebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers(x =>
        {
            x.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build()));
        });
        builder.AddServiceDefaults();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(x =>
        {
            x.DefaultResponseReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
            x.Title = "MoneySpot6 API";
        });
        builder.Services.AddSignalR();
        builder.AddNpgsqlDbContext<Db>("db");
        builder.Services.AddResponseCompression();
        builder.Services.Configure<HbciAdapterOptions>(builder.Configuration.GetSection("HbciAdapter"));
        builder.Services.AddServiceFromAttributes();
        builder.Services.AddHttpClient<MoneySpot6.WebApp.Features.Ui.InflationData.Import.GenesisApiClient>();
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");
                options.ClientId = builder.Configuration.GetValue<string>("Auth:ClientId");
                options.ClientSecret = builder.Configuration.GetValue<string>("Auth:ClientSecret");
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                };

                options.Events.OnRedirectToIdentityProvider = ctx =>
                {
                    if (!string.IsNullOrWhiteSpace(builder.Configuration.GetValue<string>("Domain")))
                        ctx.ProtocolMessage.RedirectUri = builder.Configuration.GetValue<string>("Domain") + "/signin-oidc";
                    return Task.CompletedTask;
                };
            });
        builder.Services.Configure<MailIntegrationOptions>(builder.Configuration.GetSection("MailIntegration"));
        builder.Services.Configure<InflationImportOptions>(builder.Configuration.GetSection("InflationImport"));
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseResponseCompression();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapDefaultEndpoints();
        app.MapHub<AccountSyncHub>("/api/account-sync");
        app.MapFallbackToFile("/index.html");

        if (await app.Services.CreateTypeScriptClient(args))
            return;

        using (var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<Db>().Database.MigrateAsync();

        await app.RunAsync();
    }
}

public class MailIntegrationOptions
{
    public string? GmailClientId { get; init; }
    public string? GmailClientSecret { get; init; }
    public string? OpenAIApiKey { get; init; }
}

public class InflationImportOptions
{
    public string? GenesisApiToken { get; init; }
}