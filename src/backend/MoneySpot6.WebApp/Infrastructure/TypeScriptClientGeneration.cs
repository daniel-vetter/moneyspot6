using NSwag.CodeGeneration.TypeScript;
using NSwag.Generation;
using NSwag;

namespace MoneySpot6.WebApp.Infrastructure
{
    public static class TypeScriptClientGeneration
    {
        public static async Task<bool> CreateTypeScriptClient(this IServiceProvider sp, string[] args)
        {
            var index = Array.IndexOf(args, "--generateTypeScriptClient");
            if (index == -1)
                return false;
            
            var document = await sp.GetRequiredService<IOpenApiDocumentGenerator>().GenerateAsync("v1");
            var typescript = new TypeScriptClientGenerator(await OpenApiDocument.FromJsonAsync(document.ToJson()), new TypeScriptClientGeneratorSettings()).GenerateFile();
            await File.WriteAllTextAsync(args[index+1], typescript);
            Console.WriteLine("TypeScript client written to: " + Path.GetFullPath(args[index + 1]));
            return true;
        }
    }
}
