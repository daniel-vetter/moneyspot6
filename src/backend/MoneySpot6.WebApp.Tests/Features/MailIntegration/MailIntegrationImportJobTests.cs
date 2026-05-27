using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core;
using MoneySpot6.WebApp.Features.Core.MailIntegration;
using MoneySpot6.WebApp.Tests.Api;
using Shouldly;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Tests.Features.MailIntegration;

public class MailIntegrationImportJobTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private FakeMailProvider _mailProvider = null!;

    [SetUp]
    public void SetUpFakeProvider()
    {
        _mailProvider = new FakeMailProvider();
    }

    private MailIntegrationImportJob CreateJob() => new(
        Get<Db>(),
        _mailProvider,
        NullLogger<MailIntegrationImportJob>.Instance,
        Services.GetRequiredService<WaitHelper>());

    private async Task<DbGMailIntegration> CreateAccount(string name)
    {
        var account = new DbGMailIntegration
        {
            Name = name,
            AccessToken = "fake",
            RefreshToken = "fake",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        Get<Db>().Add(account);
        await Get<Db>().SaveChangesAsync();
        return account;
    }

    private async Task<DbMonitoredEmailAddress> CreateMonitoredAddress(string address)
    {
        var monitored = new DbMonitoredEmailAddress { EmailAddress = address };
        Get<Db>().Add(monitored);
        await Get<Db>().SaveChangesAsync();
        return monitored;
    }

    private static EmailData Mail(string id, DateTimeOffset internalDate, string from = "sender@example.com")
        => new()
        {
            Id = id,
            InternalDate = internalDate,
            From = from,
            Subject = $"Subject {id}",
            Body = $"Body {id}"
        };

    private void ConfigureAccount(DbGMailIntegration account)
    {
        _mailProvider.Accounts = _mailProvider.Accounts.Add(new GMailAccountInfo
        {
            Id = account.Id,
            EmailAddress = account.Name
        });
    }

    [Test]
    public async Task Update_WithNoMonitoredAddresses_DoesNotPersistAnyJob()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);

        await CreateJob().Update(CancellationToken.None);

        (await Get<Db>().Set<DbEmailSyncJob>().CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Update_WritesOneJobPerAccount_NotPerMonitoredAddress()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        await CreateMonitoredAddress("shop@example.com");
        await CreateMonitoredAddress("rechnung@example.com");

        await CreateJob().Update(CancellationToken.None);

        var jobs = await Get<Db>().Set<DbEmailSyncJob>().ToListAsync();
        jobs.Count.ShouldBe(1);
        jobs[0].ErrorMessage.ShouldBeNull();
    }

    [Test]
    public async Task Update_AggregatesImportedCountAcrossMonitoredAddresses()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        var shop = await CreateMonitoredAddress("shop@example.com");
        var rechnung = await CreateMonitoredAddress("rechnung@example.com");

        _mailProvider.Mails[(account.Id, shop.EmailAddress)] =
            [Mail("a", DateTimeOffset.UtcNow), Mail("b", DateTimeOffset.UtcNow)];
        _mailProvider.Mails[(account.Id, rechnung.EmailAddress)] =
            [Mail("c", DateTimeOffset.UtcNow)];

        await CreateJob().Update(CancellationToken.None);

        var job = await Get<Db>().Set<DbEmailSyncJob>().SingleAsync();
        job.ImportedEmailCount.ShouldBe(3);
    }

    [Test]
    public async Task Update_OneAddressFails_OtherAddressesStillRun()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        var failing = await CreateMonitoredAddress("failing@example.com");
        var working = await CreateMonitoredAddress("working@example.com");

        _mailProvider.FailFor[(account.Id, failing.EmailAddress)] = new InvalidOperationException("Boom");
        _mailProvider.Mails[(account.Id, working.EmailAddress)] = [Mail("x", DateTimeOffset.UtcNow)];

        await CreateJob().Update(CancellationToken.None);

        var job = await Get<Db>().Set<DbEmailSyncJob>().SingleAsync();
        job.ImportedEmailCount.ShouldBe(1); // working address still imported
        job.ErrorMessage.ShouldNotBeNull();
    }

    [Test]
    public async Task Update_FailingAddress_ErrorMessageIsPrefixedWithAddress()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        var failing = await CreateMonitoredAddress("failing@example.com");

        _mailProvider.FailFor[(account.Id, failing.EmailAddress)] = new InvalidOperationException("Token expired");

        await CreateJob().Update(CancellationToken.None);

        var job = await Get<Db>().Set<DbEmailSyncJob>().SingleAsync();
        job.ErrorMessage.ShouldBe("[failing@example.com] Token expired");
    }

    [Test]
    public async Task Update_AllAddressesSucceed_JobHasNullError()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        await CreateMonitoredAddress("shop@example.com");

        await CreateJob().Update(CancellationToken.None);

        var job = await Get<Db>().Set<DbEmailSyncJob>().SingleAsync();
        job.ErrorMessage.ShouldBeNull();
    }

    [Test]
    public async Task Update_MoreThan10JobsPerAccount_PrunesOldest()
    {
        var account = await CreateAccount("acc1@example.com");
        ConfigureAccount(account);
        await CreateMonitoredAddress("shop@example.com");

        // Seed 12 historical jobs for this account so the next Update tick exceeds 10 → pruning kicks in.
        var baseTime = DateTimeOffset.UtcNow.AddDays(-1);
        for (int i = 0; i < 12; i++)
        {
            Get<Db>().Add(new DbEmailSyncJob
            {
                GMailAccount = account,
                StartedAt = baseTime.AddMinutes(i),
                FinishedAt = baseTime.AddMinutes(i).AddSeconds(1),
                ErrorMessage = null,
                ImportedEmailCount = 0
            });
        }
        await Get<Db>().SaveChangesAsync();

        await CreateJob().Update(CancellationToken.None);

        var remaining = await Get<Db>().Set<DbEmailSyncJob>()
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync();

        remaining.Count.ShouldBe(10);
        // The new run from Update is the newest job and must survive
        remaining[0].StartedAt.ShouldBeGreaterThan(baseTime.AddMinutes(11));
    }

    [Test]
    public async Task Update_PruningRespectsPerAccountQuota()
    {
        var account1 = await CreateAccount("acc1@example.com");
        var account2 = await CreateAccount("acc2@example.com");
        ConfigureAccount(account1);
        ConfigureAccount(account2);
        await CreateMonitoredAddress("shop@example.com");

        // 15 jobs for account1, only 2 for account2.
        var baseTime = DateTimeOffset.UtcNow.AddDays(-1);
        for (int i = 0; i < 15; i++)
        {
            Get<Db>().Add(new DbEmailSyncJob
            {
                GMailAccount = account1,
                StartedAt = baseTime.AddMinutes(i),
                FinishedAt = baseTime.AddMinutes(i).AddSeconds(1),
                ErrorMessage = null,
                ImportedEmailCount = 0
            });
        }
        for (int i = 0; i < 2; i++)
        {
            Get<Db>().Add(new DbEmailSyncJob
            {
                GMailAccount = account2,
                StartedAt = baseTime.AddMinutes(i),
                FinishedAt = baseTime.AddMinutes(i).AddSeconds(1),
                ErrorMessage = null,
                ImportedEmailCount = 0
            });
        }
        await Get<Db>().SaveChangesAsync();

        await CreateJob().Update(CancellationToken.None);

        var account1Jobs = await Get<Db>().Set<DbEmailSyncJob>()
            .Where(j => j.GMailAccount.Id == account1.Id)
            .CountAsync();
        var account2Jobs = await Get<Db>().Set<DbEmailSyncJob>()
            .Where(j => j.GMailAccount.Id == account2.Id)
            .CountAsync();

        // account1: capped at 10 (after pruning and the new tick's job)
        account1Jobs.ShouldBe(10);
        // account2: the pre-existing 2 + the new tick's job = 3, untouched by pruning
        account2Jobs.ShouldBe(3);
    }
}
