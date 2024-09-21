using MoneySpot6.WebApp.Infrastructure;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

namespace MoneySpot6.WebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(x => x.Title = "MoneySpot6 API");
        builder.Services.AddSignalR();
        builder.Services.AddDbContext<Db>(x => x.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
        builder.Services.Configure<HbciAdapterOptions>(builder.Configuration.GetSection("HbciAdapter"));

        foreach (var type in typeof(Program).Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<SingletonServiceAttribute>() != null)
                builder.Services.AddSingleton(type);
            if (type.GetCustomAttribute<ScopedServiceAttribute>() != null)
                builder.Services.AddScoped(type);
        }

        var app = builder.Build();
        if (await app.Services.CreateTypeScriptClient(args))
            return;
            
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

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute : Attribute
{
}

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute : Attribute
{
}