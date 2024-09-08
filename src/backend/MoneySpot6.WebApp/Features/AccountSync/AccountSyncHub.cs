using Microsoft.AspNetCore.SignalR;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using System.Collections.Immutable;
using MoneySpot6.WebApp.Features.AccountSync.Services;

namespace MoneySpot6.WebApp.Features.AccountSync;

public class AccountSyncHub : Hub
{
    private readonly AccountSyncService _accountSyncService;

    public AccountSyncHub(AccountSyncService accountSyncService)
    {
        _accountSyncService = accountSyncService;
    }

    public async Task Start()
    {
        var handler = new Handler(Clients.Caller);
        await _accountSyncService.Sync(handler);
    }
}

public class Handler : IAdapterCallbackHandler
{
    private readonly ISingleClientProxy _client;

    public Handler(ISingleClientProxy client)
    {
        _client = client;
    }

    public async Task<string?> OnTanRequired(string message)
    {
        var tan = await _client.InvokeCoreAsync<string?>("requestTan", [message], CancellationToken.None);
        return tan;
    }

    public async Task<string?> OnSecurityMechanismRequired(ImmutableDictionary<string, string> securityMechanism)
    {
        var code = await _client.InvokeCoreAsync<string?>("requestSecurityMechanism", [securityMechanism], CancellationToken.None);
        return code;
    }

    public async Task OnLogMessage(int severity, string message)
    {
        await _client.SendCoreAsync("logMessage", [severity, message], CancellationToken.None);
    }
}