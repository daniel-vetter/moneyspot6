using System.Collections.Immutable;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.MailIntegrationPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class MailIntegrationApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private async Task<DbGMailIntegration> CreateAccount(string name)
    {
        var account = new DbGMailIntegration
        {
            Name = name,
            AccessToken = "fake-access",
            RefreshToken = "fake-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        Get<Db>().Add(account);
        await Get<Db>().SaveChangesAsync();
        return account;
    }

    private async Task AddJob(DbGMailIntegration account, DateTimeOffset startedAt, string? errorMessage = null, int importedCount = 0)
    {
        Get<Db>().Add(new DbEmailSyncJob
        {
            GMailAccount = account,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddSeconds(5),
            ErrorMessage = errorMessage,
            ImportedEmailCount = importedCount
        });
        await Get<Db>().SaveChangesAsync();
    }

    [Test]
    public async Task GetSyncStatus_NoJobs_HasFailedSyncIsFalse()
    {
        var result = await Get<MailIntegrationController>().GetSyncStatus();

        var status = result.ShouldBeOkObjectResult<EmailSyncStatusResponse>();
        status.HasFailedSync.ShouldBeFalse();
    }

    [Test]
    public async Task GetSyncStatus_LatestJobHasError_HasFailedSyncIsTrue()
    {
        var account = await CreateAccount("acc1@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddHours(-2));
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5), errorMessage: "Token expired");

        var result = await Get<MailIntegrationController>().GetSyncStatus();

        var status = result.ShouldBeOkObjectResult<EmailSyncStatusResponse>();
        status.HasFailedSync.ShouldBeTrue();
    }

    [Test]
    public async Task GetSyncStatus_LatestJobIsSuccess_OldFailureIgnored()
    {
        var account = await CreateAccount("acc1@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddHours(-2), errorMessage: "Old failure");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = await Get<MailIntegrationController>().GetSyncStatus();

        var status = result.ShouldBeOkObjectResult<EmailSyncStatusResponse>();
        status.HasFailedSync.ShouldBeFalse();
    }

    [Test]
    public async Task GetSyncStatus_OneAccountFailedAnotherSucceeded_HasFailedSyncIsTrue()
    {
        var failingAccount = await CreateAccount("failing@example.com");
        var healthyAccount = await CreateAccount("healthy@example.com");

        await AddJob(failingAccount, DateTimeOffset.UtcNow.AddMinutes(-10), errorMessage: "Boom");
        await AddJob(healthyAccount, DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = await Get<MailIntegrationController>().GetSyncStatus();

        var status = result.ShouldBeOkObjectResult<EmailSyncStatusResponse>();
        status.HasFailedSync.ShouldBeTrue();
    }

    [Test]
    public async Task GetSyncStatus_GroupsPerAccount_NotGlobalLatest()
    {
        // Account1's latest is a failure, Account2 had a later successful run.
        // The global latest job is the successful Account2 one — but per-account,
        // Account1 is still in a failed state.
        var account1 = await CreateAccount("acc1@example.com");
        var account2 = await CreateAccount("acc2@example.com");

        await AddJob(account1, DateTimeOffset.UtcNow.AddMinutes(-10), errorMessage: "Stale failure");
        await AddJob(account2, DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await Get<MailIntegrationController>().GetSyncStatus();

        var status = result.ShouldBeOkObjectResult<EmailSyncStatusResponse>();
        status.HasFailedSync.ShouldBeTrue();
    }

    [Test]
    public async Task GetSyncJobs_NoJobs_ReturnsEmpty()
    {
        var result = await Get<MailIntegrationController>().GetSyncJobs();

        var jobs = result.ShouldBeOkObjectResult<ImmutableArray<SyncJobResponse>>();
        jobs.ShouldBeEmpty();
    }

    [Test]
    public async Task GetSyncJobs_ReturnsAllJobsSortedByStartedAtDescending()
    {
        var account = await CreateAccount("acc1@example.com");
        var oldest = DateTimeOffset.UtcNow.AddHours(-3);
        var middle = DateTimeOffset.UtcNow.AddHours(-1);
        var newest = DateTimeOffset.UtcNow.AddMinutes(-1);

        await AddJob(account, oldest, importedCount: 1);
        await AddJob(account, newest, importedCount: 3);
        await AddJob(account, middle, importedCount: 2);

        var result = await Get<MailIntegrationController>().GetSyncJobs();

        var jobs = result.ShouldBeOkObjectResult<ImmutableArray<SyncJobResponse>>();
        jobs.Length.ShouldBe(3);
        jobs[0].ImportedEmailCount.ShouldBe(3); // newest
        jobs[1].ImportedEmailCount.ShouldBe(2);
        jobs[2].ImportedEmailCount.ShouldBe(1); // oldest
    }

    [Test]
    public async Task GetSyncJobs_PopulatesAccountEmailFromNavigation()
    {
        var account = await CreateAccount("someone@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await Get<MailIntegrationController>().GetSyncJobs();

        var jobs = result.ShouldBeOkObjectResult<ImmutableArray<SyncJobResponse>>();
        jobs.ShouldHaveSingleItem().AccountEmail.ShouldBe("someone@example.com");
    }

    [Test]
    public async Task GetSyncJobs_ErrorMessageRoundTripped()
    {
        var account = await CreateAccount("acc1@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-1), errorMessage: "[shop@example.com] Token expired");

        var result = await Get<MailIntegrationController>().GetSyncJobs();

        var job = result.ShouldBeOkObjectResult<ImmutableArray<SyncJobResponse>>().ShouldHaveSingleItem();
        job.ErrorMessage.ShouldBe("[shop@example.com] Token expired");
    }
}
