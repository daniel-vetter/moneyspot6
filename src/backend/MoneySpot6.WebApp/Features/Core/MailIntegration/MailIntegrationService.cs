using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class MailIntegrationService
    {
        private readonly Db _db;
        private readonly MailProvider _mailProvider;
        private readonly ILogger<MailIntegrationService> _logger;

        public MailIntegrationService(Db db, MailProvider mailProvider, ILogger<MailIntegrationService> logger)
        {
            _db = db;
            _mailProvider = mailProvider;
            _logger = logger;
        }

        internal async Task Update(CancellationToken stoppingToken)
        {
            var allMonitoredAddresses = await _db.Set<DbMonitoredEmailAddress>()
                .AsNoTracking()
                .ToImmutableArrayAsync();

            if (allMonitoredAddresses.Length == 0)
            {
                _logger.LogInformation("No monitored email addresses configured, skipping email import");
                return;
            }

            foreach (var account in await _mailProvider.GetConfiguredAccounts())
            {
                foreach (var monitoredAddress in allMonitoredAddresses)
                {
                    try
                    {
                        await ProcessMonitoredAddress(account, monitoredAddress, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process emails for account {AccountId} ({Email}) and address {MonitoredAddress}",
                            account.Id, account.EmailAddress, monitoredAddress.EmailAddress);
                    }
                }
            }
        }

        private async Task ProcessMonitoredAddress(
            GMailAccountInfo accountInfo,
            DbMonitoredEmailAddress monitoredAddress,
            CancellationToken stoppingToken)
        {
            // Load account with tracking to update LastSyncTimestamp
            var dbAccount = await _db.Set<DbGMailIntegration>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == accountInfo.Id, stoppingToken);

            if (dbAccount == null)
            {
                _logger.LogError("Account {AccountId} not found in database", accountInfo.Id);
                return;
            }

            // Load monitored address with tracking for navigation property
            var dbMonitoredAddress = await _db.Set<DbMonitoredEmailAddress>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == monitoredAddress.Id, stoppingToken);

            if (dbMonitoredAddress == null)
            {
                _logger.LogError("Monitored address {MonitoredAddressId} not found in database", monitoredAddress.Id);
                return;
            }

            long startTimestamp = dbAccount.LastSyncTimestamp ?? 0;
            long maxTimestamp = startTimestamp;
            int importedCount = 0;

            _logger.LogInformation("Checking for mail updates for account {AccountId} ({Email}) from {MonitoredAddress} from timestamp {Timestamp}", accountInfo.Id, accountInfo.EmailAddress, monitoredAddress.EmailAddress, startTimestamp);

            await foreach (var mail in _mailProvider.GetMails(accountInfo, monitoredAddress.EmailAddress, startTimestamp))
            {
                stoppingToken.ThrowIfCancellationRequested();

                // Check if this email was already imported for this monitored address
                var existingEmail = await _db.Set<DbImportedEmail>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.MessageId == mail.Id
                        && x.GMailAccount.Id == accountInfo.Id
                        && x.MonitoredAddress.Id == monitoredAddress.Id, stoppingToken);

                if (existingEmail != null)
                {
                    _logger.LogDebug("Email {MessageId} already imported for monitored address {MonitoredAddressId}, skipping", mail.Id, monitoredAddress.Id);
                    continue;
                }

                _db.Add(new DbImportedEmail
                {
                    GMailAccount = dbAccount,
                    MonitoredAddress = dbMonitoredAddress,
                    MessageId = mail.Id,
                    InternalDate = mail.InternalDate,
                    FromAddress = mail.From,
                    Subject = mail.Subject,
                    Body = mail.Body,
                    ImportedAt = DateTimeOffset.UtcNow
                });

                importedCount++;

                if (mail.InternalDate > maxTimestamp)
                    maxTimestamp = mail.InternalDate;

                _logger.LogInformation("Imported email from {From}: {Subject} for monitored address {MonitoredAddress} (InternalDate: {InternalDate})", mail.From, mail.Subject, monitoredAddress.EmailAddress, mail.InternalDate);
            }

            if (maxTimestamp > startTimestamp)
            {
                dbAccount.LastSyncTimestamp = maxTimestamp;
                _logger.LogInformation("Updated LastSyncTimestamp for account {AccountId} to {Timestamp}", accountInfo.Id, maxTimestamp);
            }

            await _db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Completed email import for account {AccountId} ({Email}) and monitored address {MonitoredAddress}. Imported {Count} new emails", accountInfo.Id, accountInfo.EmailAddress, monitoredAddress.EmailAddress, importedCount);
        }
    }
}