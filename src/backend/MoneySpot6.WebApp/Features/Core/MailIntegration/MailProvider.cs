using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.Collections.Immutable;
using System.Net.Mail;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    public class MailProvider
    {
        private readonly Db _db;
        private readonly IOptions<MailIntegrationOptions> _configuration;
        private readonly ILogger<MailProvider> _logger;

        public MailProvider(Db db, IOptions<MailIntegrationOptions> configuration, ILogger<MailProvider> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ImmutableArray<GMailAccountInfo>> GetConfiguredAccounts()
        {
            return await _db
                .Set<DbGMailIntegration>()
                .AsNoTracking()
                .Select(x => new GMailAccountInfo { Id = x.Id, EmailAddress = x.Name })
                .ToImmutableArrayAsync();
        }

        public async Task<GmailService> GetClient(GMailAccountInfo accountInfo)
        {
            var account = await _db
                .Set<DbGMailIntegration>()
                .AsNoTracking()
                .SingleAsync(x => x.Id == accountInfo.Id);

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration.Value.GmailClientId,
                    ClientSecret = _configuration.Value.GmailClientSecret
                },
                Scopes = [GmailService.Scope.GmailReadonly],
            });

            var credential = new UserCredential(flow, "1", new TokenResponse
            {
                RefreshToken = account.RefreshToken,
                AccessToken = account.AccessToken
            });

            var client = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MoneySpot",
            });

            return client;
        }

        public async IAsyncEnumerable<EmailData> GetMails(GMailAccountInfo account, string senderAddress, long startingTimestamp)
        {
            var client = await GetClient(account);

            var request = client.Users.Messages.List("me");
            request.MaxResults = 500;

            // Use startingTimestamp if available, otherwise default to 1 year ago
            long searchAfterSeconds = startingTimestamp > 0
                ? startingTimestamp / 1000  // Gmail InternalDate is in milliseconds, API uses seconds
                : DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds();

            request.Q = $"from:{senderAddress} after:{searchAfterSeconds}";

            string? pageToken = null;
            do
            {
                request.PageToken = pageToken;
                var response = await request.ExecuteAsync();

                if (response.Messages != null)
                {
                    _logger.LogInformation("Loaded {count} mail stubs from {AccountId} ({Email})", response.Messages.Count, account.Id, account.EmailAddress);
                    foreach (var stub in response.Messages)
                    {
                        var getReq = client.Users.Messages.Get("me", stub.Id);
                        getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                        var msg = await getReq.ExecuteAsync();

                        // Only return emails newer than the starting timestamp
                        if (msg.InternalDate.HasValue && msg.InternalDate.Value > startingTimestamp)
                        {
                            var fromHeader = GetHeader(msg, "From");
                            yield return new EmailData
                            {
                                Id = msg.Id,
                                InternalDate = msg.InternalDate.Value,
                                From = ExtractEmailAddress(fromHeader) ?? "Unknown",
                                Subject = GetHeader(msg, "Subject") ?? "(No Subject)",
                                Body = ExtractBody(msg)
                            };
                        }
                    }
                }

                pageToken = response.NextPageToken;

            } while (!string.IsNullOrEmpty(pageToken));
        }

        private string? GetHeader(Message message, string headerName)
        {
            return message.Payload?.Headers?.FirstOrDefault(h =>
                h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private string? ExtractEmailAddress(string? fromHeader)
        {
            if (string.IsNullOrWhiteSpace(fromHeader))
                return null;

            try
            {
                // Use MailAddress to parse the from header
                // This handles formats like "Name <email@example.com>" or just "email@example.com"
                var mailAddress = new MailAddress(fromHeader);
                return mailAddress.Address;
            }
            catch
            {
                // If parsing fails, try to extract email with regex as fallback
                var match = System.Text.RegularExpressions.Regex.Match(fromHeader, @"<([^>]+)>");
                if (match.Success)
                    return match.Groups[1].Value;

                // If no angle brackets, assume the whole string is the email
                return fromHeader.Trim();
            }
        }

        private string ExtractBody(Message message)
        {
            if (message.Payload == null)
                return string.Empty;

            // Try to get plain text body
            var textPart = FindPart(message.Payload, "text/plain");
            if (textPart?.Body?.Data != null)
            {
                return DecodeBase64Url(textPart.Body.Data);
            }

            // Fallback to HTML body
            var htmlPart = FindPart(message.Payload, "text/html");
            if (htmlPart?.Body?.Data != null)
            {
                return DecodeBase64Url(htmlPart.Body.Data);
            }

            // Last resort: try body data directly
            if (message.Payload.Body?.Data != null)
            {
                return DecodeBase64Url(message.Payload.Body.Data);
            }

            return string.Empty;
        }

        private MessagePart? FindPart(MessagePart part, string mimeType)
        {
            if (part.MimeType?.Equals(mimeType, StringComparison.OrdinalIgnoreCase) == true)
                return part;

            if (part.Parts != null)
            {
                foreach (var subPart in part.Parts)
                {
                    var result = FindPart(subPart, mimeType);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private string DecodeBase64Url(string base64Url)
        {
            try
            {
                // Gmail uses URL-safe base64 encoding
                string base64 = base64Url.Replace('-', '+').Replace('_', '/');

                // Add padding if needed
                int padding = base64.Length % 4;
                if (padding > 0)
                    base64 += new string('=', 4 - padding);

                var bytes = Convert.FromBase64String(base64);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public record GMailAccountInfo
    {
        public required int Id { get; init; }
        public required string EmailAddress { get; init; }
    }

    public record EmailData
    {
        public required string Id { get; init; }
        public required long InternalDate { get; init; }
        public required string From { get; init; }
        public required string Subject { get; init; }
        public required string Body { get; init; }
    }
}
