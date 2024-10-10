using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using MoneySpot6.WebApp.Infrastructure;
using NJsonSchema.Generation;

namespace MoneySpot6.WebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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