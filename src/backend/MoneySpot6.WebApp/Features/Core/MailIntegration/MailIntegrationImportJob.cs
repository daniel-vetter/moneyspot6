using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    internal class MailIntegrationImportJob
    {
        private readonly Db _db;
        private readonly MailProvider _mailProvider;
        private readonly ILogger<MailIntegrationImportJob> _logger;
        private readonly WaitHelper _waitHelper;

        public MailIntegrationImportJob(Db db, MailProvider mailProvider, ILogger<MailIntegrationImportJob> logger, WaitHelper waitHelper)
        {
            _db = db;
            _mailProvider = mailProvider;
            _logger = logger;
            _waitHelper = waitHelper;
        }

        internal async Task Update(CancellationToken stoppingToken)
        {
            var allMonitoredAddresses = await _db.Set<DbMonitoredEmailAddress>()
                .AsNoTracking()
                .ToImmutableArrayAsync();

            if (allMonitoredAddresses.Length == 0)
                return;

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
                        _logger.LogError(ex, "Failed: {Email} -> {MonitoredAddress}", account.EmailAddress, monitoredAddress.EmailAddress);
                    }
                }
            }
        }

        private async Task ProcessMonitoredAddress(GMailAccountInfo accountInfo, DbMonitoredEmailAddress monitoredAddress, CancellationToken stoppingToken)
        {
            // Load account for navigation property
            var dbAccount = await _db.Set<DbGMailIntegration>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == accountInfo.Id, stoppingToken);

            if (dbAccount == null)
            {
                _logger.LogError("Account {AccountId} not found in database", accountInfo.Id);
                return;
            }

            // Load monitored address for navigation property
            var dbMonitoredAddress = await _db.Set<DbMonitoredEmailAddress>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == monitoredAddress.Id, stoppingToken);

            if (dbMonitoredAddress == null)
            {
                _logger.LogError("Monitored address {MonitoredAddressId} not found in database", monitoredAddress.Id);
                return;
            }

            // Load or create sync status for this account + monitored address combination
            var syncStatus = await _db.Set<DbEmailSyncStatus>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.GMailAccount.Id == accountInfo.Id && x.MonitoredAddress.Id == monitoredAddress.Id, stoppingToken);

            if (syncStatus == null)
            {
                syncStatus = new DbEmailSyncStatus
                {
                    GMailAccount = dbAccount,
                    MonitoredAddress = dbMonitoredAddress,
                    LastSyncTimestamp = DateTimeOffset.UtcNow.AddYears(-1)
                };
                _db.Add(syncStatus);
            }

            DateTimeOffset startTimestamp = syncStatus.LastSyncTimestamp;
            DateTimeOffset maxTimestamp = startTimestamp;
            int importedCount = 0;

            _logger.LogInformation("Checking mails: {Email} -> {MonitoredAddress} (ts: {Timestamp})", accountInfo.EmailAddress, monitoredAddress.EmailAddress, startTimestamp);

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
                    continue;

                _db.Add(new DbImportedEmail
                {
                    GMailAccount = dbAccount,
                    MonitoredAddress = dbMonitoredAddress,
                    MessageId = mail.Id,
                    InternalDate = mail.InternalDate.ToUniversalTime(),
                    FromAddress = mail.From,
                    Subject = mail.Subject,
                    Body = mail.Body,
                    ImportedAt = DateTimeOffset.UtcNow
                });

                importedCount++;

                if (mail.InternalDate > maxTimestamp)
                    maxTimestamp = mail.InternalDate;

                await _db.SaveChangesAsync(stoppingToken);
                _waitHelper.Trigger<EmailProcessingBackgroundWorker>();
                _logger.LogInformation("Imported: {From} - {Subject}", mail.From, mail.Subject);
            }

            if (maxTimestamp > startTimestamp)
            {
                syncStatus.LastSyncTimestamp = maxTimestamp;
                _logger.LogInformation("Updated sync timestamp: {MonitoredAddress} -> {Timestamp}", monitoredAddress.EmailAddress, maxTimestamp);
            }

            await _db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Completed: {Email} -> {MonitoredAddress} ({Count} emails)", accountInfo.EmailAddress, monitoredAddress.EmailAddress, importedCount);
        }
    }
}