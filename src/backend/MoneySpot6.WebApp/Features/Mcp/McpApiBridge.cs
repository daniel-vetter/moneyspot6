using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MoneySpot6.WebApp.Features.Mcp;

/// <summary>
/// Bridges the existing MVC controllers to MCP. It discovers every action marked with <see cref="McpToolAttribute"/>,
/// derives a tool (name, description, input schema) from the action's route and parameters, and on invocation
/// reconstructs the HTTP request and calls the endpoint in-process. No logic is duplicated: model binding,
/// validation and serialization all run through the real endpoint.
/// </summary>
public sealed class McpApiBridge
{
    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly object _catalogLock = new();
    private ImmutableArray<ToolBinding>? _catalog;

    public McpApiBridge(IApiDescriptionGroupCollectionProvider apiDescriptions, IHttpClientFactory httpClientFactory)
    {
        _apiDescriptions = apiDescriptions;
        _httpClientFactory = httpClientFactory;
    }

    public ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        var tools = GetCatalog().Select(b => b.Tool).ToList();
        return new ValueTask<ListToolsResult>(new ListToolsResult { Tools = tools });
    }

    public async ValueTask<CallToolResult> CallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var name = request.Params?.Name;
        var binding = GetCatalog().FirstOrDefault(b => b.Tool.Name == name);
        if (binding is null)
            return Error($"Unknown tool '{name}'.");

        var arguments = request.Params?.Arguments ?? new Dictionary<string, JsonElement>();
        var httpRequest = BuildHttpRequest(binding, arguments);

        var client = _httpClientFactory.CreateClient(McpModule.SelfHttpClientName);
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonElement? structured = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                // Only objects are valid MCP structured content; scalars/arrays stay text-only.
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                    structured = document.RootElement.Clone();
            }
            catch (JsonException) { /* not JSON — text only */ }
        }

        var text = string.IsNullOrWhiteSpace(body)
            ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
            : body;

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }],
            StructuredContent = structured,
            IsError = !response.IsSuccessStatusCode,
        };
    }

    private HttpRequestMessage BuildHttpRequest(ToolBinding binding, IDictionary<string, JsonElement> arguments)
    {
        var path = binding.RelativePath;
        var query = new List<string>();
        HttpContent? content = null;

        foreach (var parameter in binding.Parameters)
        {
            if (!arguments.TryGetValue(parameter.Name, out var value) || value.ValueKind == JsonValueKind.Undefined)
                continue;

            switch (parameter.Source)
            {
                case ParameterSource.Path:
                    path = path.Replace($"{{{parameter.Name}}}", Uri.EscapeDataString(ScalarToString(value) ?? ""));
                    break;
                case ParameterSource.Query:
                    var scalar = ScalarToString(value);
                    if (scalar is not null)
                        query.Add($"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(scalar)}");
                    break;
                case ParameterSource.Body:
                    content = JsonContent.Create(value, value.GetType());
                    break;
            }
        }

        var url = query.Count > 0 ? $"{path}?{string.Join("&", query)}" : path;
        return new HttpRequestMessage(new HttpMethod(binding.HttpMethod), url) { Content = content };
    }

    private static string? ScalarToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => element.GetRawText(),
    };

    private static CallToolResult Error(string message) => new()
    {
        Content = [new TextContentBlock { Text = message }],
        IsError = true,
    };

    private ImmutableArray<ToolBinding> GetCatalog()
    {
        if (_catalog is { } cached)
            return cached;

        lock (_catalogLock)
        {
            _catalog ??= BuildCatalog();
            return _catalog.Value;
        }
    }

    private ImmutableArray<ToolBinding> BuildCatalog()
    {
        var builder = ImmutableArray.CreateBuilder<ToolBinding>();

        foreach (var group in _apiDescriptions.ApiDescriptionGroups.Items)
        {
            foreach (var api in group.Items)
            {
                if (api.ActionDescriptor is not ControllerActionDescriptor action)
                    continue;

                var attribute = action.MethodInfo.GetCustomAttributes(typeof(McpToolAttribute), inherit: false)
                    .OfType<McpToolAttribute>()
                    .FirstOrDefault();
                if (attribute is null)
                    continue;

                if (string.IsNullOrEmpty(api.RelativePath) || string.IsNullOrEmpty(api.HttpMethod))
                    continue;

                var name = attribute.Name ?? $"{action.ControllerName}_{action.ActionName}";
                var description = attribute.Description ?? DescriptionFromMetadata(api) ?? "";

                var parameters = ExtractParameters(api);
                var tool = new Tool
                {
                    Name = name,
                    Description = description,
                    InputSchema = BuildInputSchema(parameters),
                };

                builder.Add(new ToolBinding(tool, api.RelativePath, api.HttpMethod, parameters));
            }
        }

        return builder.ToImmutable();
    }

    private static string? DescriptionFromMetadata(ApiDescription api)
    {
        foreach (var metadata in api.ActionDescriptor.EndpointMetadata)
        {
            if (metadata is IEndpointDescriptionMetadata { Description: { Length: > 0 } d })
                return d;
            if (metadata is IEndpointSummaryMetadata { Summary: { Length: > 0 } s })
                return s;
        }

        return null;
    }

    private static ImmutableArray<ToolParameter> ExtractParameters(ApiDescription api)
    {
        var builder = ImmutableArray.CreateBuilder<ToolParameter>();
        foreach (var parameter in api.ParameterDescriptions)
        {
            var source = MapSource(parameter.Source);
            if (source is not { } mapped)
                continue;

            builder.Add(new ToolParameter(parameter.Name, mapped, parameter.Type, parameter.IsRequired));
        }

        return builder.ToImmutable();
    }

    private static ParameterSource? MapSource(BindingSource source)
    {
        if (source == BindingSource.Path) return ParameterSource.Path;
        if (source == BindingSource.Query) return ParameterSource.Query;
        if (source == BindingSource.Body) return ParameterSource.Body;
        return null;
    }

    private static JsonElement BuildInputSchema(ImmutableArray<ToolParameter> parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            properties[parameter.Name] = parameter.Source == ParameterSource.Body
                ? SchemaForBody(parameter.Type)
                : SchemaForScalar(parameter.Type);
            if (parameter.IsRequired)
                required.Add(parameter.Name);
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required.Count > 0)
            schema["required"] = required;

        return JsonSerializer.SerializeToElement(schema);
    }

    private static JsonNode SchemaForBody(Type type)
    {
        var schema = NJsonSchema.JsonSchema.FromType(type);
        return JsonNode.Parse(schema.ToJson()) ?? new JsonObject { ["type"] = "object" };
    }

    private static JsonNode SchemaForScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t.IsEnum)
        {
            var values = new JsonArray();
            foreach (var enumName in Enum.GetNames(t))
                values.Add(enumName);
            return new JsonObject { ["type"] = "string", ["enum"] = values };
        }

        if (t == typeof(bool))
            return new JsonObject { ["type"] = "boolean" };
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
            return new JsonObject { ["type"] = "integer" };
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return new JsonObject { ["type"] = "number" };
        if (t == typeof(DateOnly))
            return new JsonObject { ["type"] = "string", ["format"] = "date" };
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            return new JsonObject { ["type"] = "string", ["format"] = "date-time" };
        if (t == typeof(Guid))
            return new JsonObject { ["type"] = "string", ["format"] = "uuid" };

        return new JsonObject { ["type"] = "string" };
    }

    private enum ParameterSource { Path, Query, Body }

    private sealed record ToolParameter(string Name, ParameterSource Source, Type Type, bool IsRequired);

    private sealed record ToolBinding(Tool Tool, string RelativePath, string HttpMethod, ImmutableArray<ToolParameter> Parameters);
}
