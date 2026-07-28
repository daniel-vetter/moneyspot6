using System.Text.Json;
using MoneySpot6.WebApp.Features.Core.Config;

namespace MoneySpot6.WebApp.Features.Core.Gate;

[ScopedService]
public class GateService
{
    public const string UrlConfigKey = "Gate.Url";
    public const string EnabledConfigKey = "Gate.Enabled";

    private static readonly TimeSpan CheckDeadline = TimeSpan.FromSeconds(4);

    private readonly HttpClient _httpClient;
    private readonly KeyValueConfiguration _config;
    private readonly ILogger<GateService> _logger;

    public GateService(HttpClient httpClient, KeyValueConfiguration config, ILogger<GateService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<GateConfig> GetConfig()
    {
        var url = await _config.Get(UrlConfigKey, "");
        var enabled = await _config.Get(EnabledConfigKey, false);
        return new GateConfig(url, enabled);
    }

    public async Task SetConfig(string url, bool enabled)
    {
        await _config.Set(UrlConfigKey, url ?? "");
        await _config.Set(EnabledConfigKey, enabled);
    }

    public async Task<GateCheckResult> Check()
    {
        var config = await GetConfig();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.Url))
            return GateCheckResult.Allowed;

        try
        {
            using var cts = new CancellationTokenSource(CheckDeadline);
            var response = await _httpClient.GetAsync(config.Url, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            return InterpretResponse((int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            // Fail-open: any timeout / network / unexpected error keeps the app usable.
            _logger.LogWarning(ex, "Gate check request failed; failing open.");
            return GateCheckResult.Allowed;
        }
    }

    /// <summary>
    /// Pure interpretation of an HTTP response (status + body) into a gate result.
    /// Only an explicit JSON <c>{ "blocked": true }</c> on a 2xx response blocks;
    /// everything else fails open.
    /// </summary>
    public static GateCheckResult InterpretResponse(int statusCode, string? body)
    {
        if (statusCode is < 200 or >= 300 || string.IsNullOrWhiteSpace(body))
            return GateCheckResult.Allowed;

        GateResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GateResponseDto>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return GateCheckResult.Allowed;
        }

        if (dto is not { Blocked: true })
            return GateCheckResult.Allowed;

        // Message is optional even when blocked; pass it through as-is (may be null).
        return new GateCheckResult(true, dto.Message);
    }

    private class GateResponseDto
    {
        public bool Blocked { get; set; }
        public string? Message { get; set; }
    }
}

public record GateConfig(string Url, bool Enabled);

public record GateCheckResult(bool Blocked, string? Message)
{
    public static readonly GateCheckResult Allowed = new(false, null);
}
