using System.Collections.Immutable;
using MoneySpot6.WebApp.Features.Core.MailIntegration;

namespace MoneySpot6.WebApp.Tests.Features.MailIntegration;

public class FakeMailProvider : IMailProvider
{
    public ImmutableArray<GMailAccountInfo> Accounts { get; set; } = [];

    // Keyed by (accountId, senderAddress). If a key is missing, an empty result is returned.
    public Dictionary<(int AccountId, string Sender), List<EmailData>> Mails { get; } = new();

    // Keyed by (accountId, senderAddress). When present, GetMails throws this exception
    // before yielding any messages.
    public Dictionary<(int AccountId, string Sender), Exception> FailFor { get; } = new();

    public Task<ImmutableArray<GMailAccountInfo>> GetConfiguredAccounts() => Task.FromResult(Accounts);

    public async IAsyncEnumerable<EmailData> GetMails(GMailAccountInfo account, string senderAddress, DateTimeOffset? startingTimestamp)
    {
        var key = (account.Id, senderAddress);
        if (FailFor.TryGetValue(key, out var ex))
            throw ex;

        if (Mails.TryGetValue(key, out var list))
        {
            foreach (var mail in list)
            {
                if (startingTimestamp == null || mail.InternalDate > startingTimestamp)
                    yield return mail;
            }
        }

        await Task.CompletedTask;
    }
}
