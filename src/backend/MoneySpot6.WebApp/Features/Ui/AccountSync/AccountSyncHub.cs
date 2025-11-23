using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using MoneySpot6.WebApp.Features.Core.AccountSync;
using MoneySpot6.WebApp.Features.Core.AccountSync.Adapter;

namespace MoneySpot6.WebApp.Features.Ui.AccountSync;

public class AccountSyncHub : Hub
{
    private readonly AccountSyncService _accountSyncService;
    private readonly ILogger<AccountSyncService> _logger;

    public AccountSyncHub(AccountSyncService accountSyncService, ILogger<AccountSyncService> logger)
    {
        _accountSyncService = accountSyncService;
        _logger = logger;
    }

    public async Task<SyncResult> Start()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var handler = new Handler(Clients.Caller);

        try
        {
            var newTransactionIds = await _accountSyncService.SyncAll(handler, cts.Token);
            return new SyncResult(false, null, newTransactionIds);
        }
        catch (CanceledByUserException)
        {
            return new SyncResult(true, null, ImmutableArray<int>.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Account sync failed: " + e.Message);
            return new SyncResult(false, e.Message, ImmutableArray<int>.Empty);
        }
    }

    [PublicAPI]
    public record SyncResult(bool CanceledByUser, string? Error, ImmutableArray<int> NewTransactions);
}

public class Handler : IAdapterCallbackHandler
{
    private readonly ISingleClientProxy _client;

    public Handler(ISingleClientProxy client)
    {
        _client = client;
    }

    public async Task<string?> OnTanRequired(string message, CancellationToken ct)
    {
        try
        {
            return (await _client.InvokeCoreAsync<TanResponse>("requestTan", [message], ct)).Tan;
        }
        catch (OperationCanceledException)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await _client.SendCoreAsync("requestTanCanceled", [], timeout.Token);
            return null;
        }
    }

    public async Task<string?> OnSecurityMechanismRequired(ImmutableDictionary<string, string> securityMechanism, CancellationToken ct)
    {
        try
        {
            return await _client.InvokeCoreAsync<string?>("requestSecurityMechanism", [securityMechanism], ct);
        }
        catch (OperationCanceledException)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await _client.InvokeCoreAsync<string?>("requestSecurityMechanismCanceled", [securityMechanism], timeout.Token);
            return null;
        }
    }

    public async Task OnLogMessage(int severity, string message, CancellationToken ct)
    {
        await _client.SendCoreAsync("logMessage", [severity, message], ct);
    }

    [PublicAPI]
    private record TanResponse(string? Tan);
}