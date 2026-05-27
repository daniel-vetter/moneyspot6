using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public class MailIntegrationUiTests(DbProvider dbProvider) : UiTest(dbProvider)
{
    [SetUp]
    public async Task ResetMailIntegrationState()
    {
        await _db.Set<DbEmailSyncJob>().ExecuteDeleteAsync();
        await _db.Set<DbImportedEmail>().ExecuteDeleteAsync();
        await _db.Set<DbEmailSyncStatus>().ExecuteDeleteAsync();
        await _db.Set<DbMonitoredEmailAddress>().ExecuteDeleteAsync();
        await _db.Set<DbGMailIntegration>().ExecuteDeleteAsync();
    }

    private async Task<DbGMailIntegration> CreateAccount(string name)
    {
        var account = new DbGMailIntegration
        {
            Name = name,
            AccessToken = "fake",
            RefreshToken = "fake",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _db.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task AddJob(DbGMailIntegration account, DateTimeOffset startedAt, string? errorMessage = null, int importedCount = 0)
    {
        _db.Add(new DbEmailSyncJob
        {
            GMailAccount = account,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddSeconds(5),
            ErrorMessage = errorMessage,
            ImportedEmailCount = importedCount
        });
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task SyncVerlaufButton_NoFailures_HasNoWarnIcon()
    {
        var account = await CreateAccount("ok@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5), importedCount: 1);

        await Page.GotoAsync("/settings/mail-integration");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("sync-jobs-button")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("sync-warn-icon")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task SyncVerlaufButton_WithFailure_ShowsWarnIcon()
    {
        var account = await CreateAccount("failing@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5), errorMessage: "Token expired");

        await Page.GotoAsync("/settings/mail-integration");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("sync-warn-icon")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SyncJobsDialog_OpensAndShowsRows()
    {
        var account = await CreateAccount("acc@example.com");
        var jobTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        await AddJob(account, jobTime, importedCount: 2);

        await Page.GotoAsync("/settings/mail-integration");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("sync-jobs-button").ClickAsync();

        await Expect(Page.GetByTestId("sync-jobs-table")).ToBeVisibleAsync();
        // The dialog table must contain exactly one row scoped to its body
        await Expect(Page.Locator("[data-testid='sync-jobs-table'] tbody tr")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task SettingsMenuBadge_VisibleWhenSyncFailed()
    {
        var account = await CreateAccount("failing@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5), errorMessage: "Boom");

        await Page.GotoAsync("/settings/mail-integration");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("settings-menu-badge")).ToBeVisibleAsync();
    }

    [Test]
    public async Task MailIntegrationSettingsBadge_VisibleWhenSyncFailed()
    {
        var account = await CreateAccount("failing@example.com");
        await AddJob(account, DateTimeOffset.UtcNow.AddMinutes(-5), errorMessage: "Boom");

        await Page.GotoAsync("/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("mail-integration-settings-badge")).ToBeVisibleAsync();
    }
}
