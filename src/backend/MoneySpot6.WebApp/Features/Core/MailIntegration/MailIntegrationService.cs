using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.Latency;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.Diagnostics.Metrics;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class MailIntegrationService
    {
        private readonly Db _db;
        private readonly MailAccountProvider _mailAccountProvider;
        private readonly ILogger<MailIntegrationService> _logger;

        public MailIntegrationService(Db db, MailAccountProvider mailAccountProvider, ILogger<MailIntegrationService> logger)
        {
            _db = db;
            _mailAccountProvider = mailAccountProvider;
            _logger = logger;
        }

        internal async Task Update(CancellationToken stoppingToken)
        {
            var allMonitoredAddresses = await _db.Set<DbMonitoredEmailAddress>()
                .AsNoTracking()
                .ToImmutableArrayAsync();

            if (allMonitoredAddresses.Length == 0)
                return;

            foreach (var account in await _mailAccountProvider.GetConfiguredAccounts())
            {
                _logger.LogInformation("Checking for mail updates for account {AccountId} ({Email})", account.Id, account.EmailAddress);
                await foreach (var mail in GetMails(account, allMonitoredAddresses.Select(x => x.EmailAddress).ToArray(), 0))
                {

                }
            }
        }

        public async IAsyncEnumerable<string> GetMails(GMailAccountInfo account, string[] senderAddresses, long startingTimestamp)
        {
            var client = await _mailAccountProvider.GetClient(account);

            var request = client.Users.Messages.List("me");
            request.MaxResults = 500;

            var senderMailFilter = string.Join(" OR ", senderAddresses.Select(x => $"from: {x}").ToArray());
            var cutOffDate = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
            var timeFilter = $"after:{cutOffDate}";
            request.Q = $"{senderMailFilter} AND after:{cutOffDate}";

            string? pageToken = null;
            long maxInternalDate = 0;
            do
            {
                request.PageToken = pageToken;
                var response = await request.ExecuteAsync();

                if (response.Messages != null)
                {
                    _logger.LogInformation("Loaded {count} mails from {AccountId} ({Email})", response.Messages.Count, account.Id, account.EmailAddress);
                    foreach (var stub in response.Messages)
                    {
                        var getReq = client.Users.Messages.Get("me", stub.Id);
                        getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                        var msg = await getReq.ExecuteAsync();

                        yield return msg.Raw;

                        if (msg.InternalDate.HasValue && msg.InternalDate.Value > maxInternalDate)
                            maxInternalDate = msg.InternalDate.Value;
                    }
                }

                pageToken = response.NextPageToken;

            } while (!string.IsNullOrEmpty(pageToken));
        }
    }
}