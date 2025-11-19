using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Infrastructure;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class OpenAIService
    {
        private readonly IOptions<MailIntegrationOptions> _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly HttpClient _httpClient;

        public OpenAIService(IOptions<MailIntegrationOptions> configuration, ILogger<OpenAIService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<OpenAIResult> SendAsync(OpenAIMessage[] messages, object? responseFormat = null, double temperature = 0.1, string model = "gpt-4o-mini", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_configuration.Value.OpenAIApiKey))
            {
                return OpenAIResult.Failed("OpenAI API key not configured");
            }

            try
            {
                var request = new OpenAIRequest
                {
                    Model = model,
                    Messages = messages,
                    Temperature = temperature,
                    ResponseFormat = responseFormat
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.Value.OpenAIApiKey);
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseBody);
                    return OpenAIResult.Failed($"OpenAI API error: {response.StatusCode}");
                }

                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (openAIResponse?.Choices == null || openAIResponse.Choices.Length == 0)
                {
                    return OpenAIResult.Failed("No response from OpenAI");
                }

                var content = openAIResponse.Choices[0].Message.Content;
                return OpenAIResult.Success(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling OpenAI");
                return OpenAIResult.Failed($"Network error: {ex.Message}", isTransient: true);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI request timeout");
                return OpenAIResult.Failed("Request timeout", isTransient: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI");
                return OpenAIResult.Failed($"Unexpected error: {ex.Message}");
            }
        }
    }

    public record OpenAIResult(bool IsSuccess, string? Data, string? Error, bool IsTransient)
    {
        public static OpenAIResult Success(string data) => new(true, data, null, false);
        public static OpenAIResult Failed(string error, bool isTransient = false) => new(false, null, error, isTransient);
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }

    internal class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required OpenAIMessage[] Messages { get; init; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }

        [JsonPropertyName("response_format")]
        public object? ResponseFormat { get; init; }
    }

    internal class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public OpenAIChoice[]? Choices { get; init; }
    }

    internal class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public required OpenAIMessage Message { get; init; }
    }
}
