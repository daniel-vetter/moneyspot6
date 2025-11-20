using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class EmailProcessingService
    {
        private readonly Db _db;
        private readonly IOptions<MailIntegrationOptions> _configuration;
        private readonly ILogger<EmailProcessingService> _logger;
        private const int MaxRetries = 3;

        public EmailProcessingService(Db db, IOptions<MailIntegrationOptions> configuration, ILogger<EmailProcessingService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        internal async Task ProcessEmails(CancellationToken stoppingToken)
        {
            var unprocessedEmails = await _db.Set<DbImportedEmail>()
                .Include(x => x.MonitoredAddress)
                .AsTracking()
                .Where(x => x.ProcessedAt == null && x.ProcessingError == null)
                .OrderBy(x => x.InternalDate)
                .ToImmutableArrayAsync(stoppingToken);

            if (unprocessedEmails.Length == 0)
                return;

            _logger.LogInformation("Processing {Count} unprocessed emails", unprocessedEmails.Length);

            foreach (var email in unprocessedEmails)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessEmail(email, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email {EmailId}", email.Id);
                }
            }
        }

        private async Task ProcessEmail(DbImportedEmail email, CancellationToken stoppingToken)
        {
            email.ProcessingAttempts++;

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("""
                    You are a data extraction assistant. Extract the requested information from the email and return it as valid JSON.
                    If information is not available, omit that field. Always respond with valid JSON only, no additional text.

                    Use the following json schema:

                    {
                      "recipientName": string | undefined,
                      "merchant": string | undefined,
                      "transactionTimestamp": string | undefined, // iso 8601
                      "orderNumber": string | undefined,
                      "tax": number | undefined,
                      "totalAmount": number | undefined,
                      "paymentMethod": string | undefined,
                      "accountNumber": string | undefined,
                      "transactionCode": string | undefined,
                      "items": [{
                          fullName: string | undefined,  // The name as displayed in the mail
                          shortName: string | undefined, // A short name if the fullName is too long (over 20 characters)
                          subTotal: number | undefined
                      }]
                    }
                    """),
                ChatMessage.CreateUserMessage(email.Body)
            };

            try
            {

                var apiKey = _configuration.Value.OpenAIApiKey ?? throw new InvalidOperationException("OpenAI API key not configured");
                var chatClient = new ChatClient("gpt-4o-mini", apiKey);

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.1f,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                var response = await chatClient.CompleteChatAsync(messages, options, stoppingToken);
                var content = response.Value.Content[0].Text;
                content = content.Replace("\0", "").Replace(@"\u", @"\\u");

                var validatedData = JsonSerializer.Deserialize<DbExtractedEmailData>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (validatedData == null)
                {
                    email.ProcessingError = "JSON validation failed: Deserialization returned null";
                    _logger.LogWarning("JSON validation failed for email {EmailId}: Deserialization returned null", email.Id);
                }
                else
                {
                    email.ProcessedData = validatedData;
                    email.ProcessedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation("Processed email {EmailId}: {Subject}", email.Id, email.Subject);
                }
            }
            catch (JsonException ex)
            {
                email.ProcessingError = $"JSON validation failed: {ex.Message}";
                _logger.LogWarning("JSON validation failed for email {EmailId}: {Error}", email.Id, ex.Message);
            }
            catch (ClientResultException ex) when (ex.Status == 429 || ex.Status >= 500)
            {
                _logger.LogWarning(ex, "Transient OpenAI error for email {EmailId} (attempt {Attempt}/{Max})", email.Id, email.ProcessingAttempts, MaxRetries);

                if (email.ProcessingAttempts >= MaxRetries)
                {
                    email.ProcessingError = $"Failed after {MaxRetries} attempts: {ex.Message}";
                }
            }
            catch (ClientResultException ex)
            {
                email.ProcessingError = $"OpenAI API error: {ex.Message}";
                _logger.LogError(ex, "OpenAI API error for email {EmailId}", email.Id);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "OpenAI request timeout for email {EmailId}", email.Id);

                if (email.ProcessingAttempts >= MaxRetries)
                {
                    email.ProcessingError = $"Timeout after {MaxRetries} attempts";
                }
            }
            catch (Exception ex)
            {
                email.ProcessingError = $"Unexpected error: {ex.Message}";
                _logger.LogError(ex, "Unexpected error processing email {EmailId}", email.Id);
            }

            await _db.SaveChangesAsync(stoppingToken);
        }
    }
}
