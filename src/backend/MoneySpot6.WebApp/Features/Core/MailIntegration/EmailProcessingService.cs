using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class EmailProcessingService
    {
        private readonly Db _db;
        private readonly OpenAIService _openAIService;
        private readonly ILogger<EmailProcessingService> _logger;
        private const int MaxRetries = 3;

        public EmailProcessingService(Db db, OpenAIService openAIService, ILogger<EmailProcessingService> logger)
        {
            _db = db;
            _openAIService = openAIService;
            _logger = logger;
        }

        internal async Task ProcessEmails(CancellationToken stoppingToken)
        {
            var unprocessedEmails = await _db.Set<DbImportedEmail>()
                .Include(x => x.MonitoredAddress)
                .AsTracking()
                .Where(x => x.ProcessedData == null && x.ProcessingError == null)
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

            var messages = new[]
            {
                new OpenAIMessage
                {
                    Role = "system",
                    Content = """
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
                              """
                },
                new OpenAIMessage
                {
                    Role = "user",
                    Content = email.Body
                }
            };

            var result = await _openAIService.SendAsync(
                messages,
                responseFormat: new { type = "json_object" },
                cancellationToken: stoppingToken);

            if (result.IsSuccess)
            {
                try
                {
                    var validatedData = JsonSerializer.Deserialize<ExtractedEmailData>(result.Data!);
                    if (validatedData == null)
                    {
                        email.ProcessingError = "JSON validation failed: Deserialization returned null";
                        _logger.LogWarning("JSON validation failed for email {EmailId}: Deserialization returned null", email.Id);
                    }
                    else
                    {
                        email.ProcessedData = result.Data;
                        email.ProcessedAt = DateTimeOffset.UtcNow;
                        _logger.LogInformation("Processed email {EmailId}: {Subject}", email.Id, email.Subject);
                    }
                }
                catch (JsonException ex)
                {
                    email.ProcessingError = $"JSON validation failed: {ex.Message}";
                    _logger.LogWarning("JSON validation failed for email {EmailId}: {Error}", email.Id, ex.Message);
                }
            }
            else
            {
                if (!result.IsTransient || email.ProcessingAttempts >= MaxRetries)
                {
                    email.ProcessingError = result.Error;
                    _logger.LogWarning("Permanent error processing email {EmailId}: {Error}", email.Id, result.Error);
                }
                else
                {
                    _logger.LogWarning("Transient error processing email {EmailId} (attempt {Attempt}/{Max}): {Error}",
                        email.Id, email.ProcessingAttempts, MaxRetries, result.Error);
                }
            }

            await _db.SaveChangesAsync(stoppingToken);
        }
    }

    internal class ExtractedEmailData
    {
        [JsonPropertyName("recipientName")]
        public string? RecipientName { get; set; }

        [JsonPropertyName("merchant")]
        public string? Merchant { get; set; }

        [JsonPropertyName("transactionTimestamp")]
        public string? TransactionTimestamp { get; set; }

        [JsonPropertyName("orderNumber")]
        public string? OrderNumber { get; set; }

        [JsonPropertyName("tax")]
        public decimal? Tax { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("transactionCode")]
        public string? TransactionCode { get; set; }

        [JsonPropertyName("items")]
        public ExtractedEmailItem[]? Items { get; set; }
    }

    internal class ExtractedEmailItem
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("subTotal")]
        public decimal? SubTotal { get; set; }
    }
}
