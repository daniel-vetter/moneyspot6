using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using MoneySpot6.WebApp.Infrastructure;
using NJsonSchema.Generation;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MoneySpot6.WebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSystemd();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(x =>
        {
            x.DefaultResponseReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
            x.Title = "MoneySpot6 API";
        });
        builder.Services.AddSignalR();
        builder.Services.AddDbContext<Db>(x => x.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
        builder.Services.AddResponseCompression();
        builder.Services.Configure<HbciAdapterOptions>(builder.Configuration.GetSection("HbciAdapter"));
        builder.Services.AddServiceFromAttributes();
        builder.Logging.AddOpenTelemetry(x =>
        {
            x.IncludeFormattedMessage = true;
        });
        builder.Logging.AddSystemdConsole();
        builder.Services.AddOpenTelemetry()
            .UseOtlpExporter()
            .WithMetrics(m => m
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation())
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation(x =>
                {
                    x.EnrichWithHttpResponseMessage = (activity, message) => activity.AddTag("net.peer.name", message.RequestMessage?.RequestUri?.Host);
                    x.EnrichWithHttpResponseMessage = (activity, message) => activity.AddTag("net.peer.name", message.RequestMessage?.RequestUri?.Host);
                })
                .AddNpgsql()
                .AddSource(AppActivitySource.Name)
                .SetSampler<AlwaysOnSampler>())
            .WithLogging();

        var app = builder.Build();
        if (await app.Services.CreateTypeScriptClient(args))
            return;

        app.UseResponseCompression();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.MapControllers();
        app.MapHub<AccountSyncHub>("/api/account-sync");
        app.MapFallbackToFile("/index.html");

        using (var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<Db>().Database.MigrateAsync();

        await app.RunAsync();
    }
}