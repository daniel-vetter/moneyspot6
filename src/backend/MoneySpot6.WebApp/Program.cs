using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApiDocument(x => x.Title = "MoneySpot6 API");

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
            app.MapFallbackToFile("/index.html");
            await app.RunAsync();
        }
    }
}
