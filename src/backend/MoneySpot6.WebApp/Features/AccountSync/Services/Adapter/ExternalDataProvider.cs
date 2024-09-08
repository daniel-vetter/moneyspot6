using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

[ScopedService]
public class ExternalDataProvider(IOptions<HbciAdapterOptions> options, ILogger<ExternalDataProvider> logger)
{
    public async Task<RpcSyncResponse> Run(int connectionId, string hbciVersion, string bankCode, string userId, string customerId, string pin, IAdapterCallbackHandler callbackHandler, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var p = new Process();
        p.StartInfo = new ProcessStartInfo();
        p.StartInfo.FileName = options.Value.FileName;
        p.StartInfo.Arguments = options.Value.Arguments;
        p.StartInfo.WorkingDirectory = options.Value.WorkingDirectory;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        if (!p.Start())
            throw new Exception("Could not start adapter");

        try
        {
            var rpc = await new RpcBridge(p.StandardOutput.BaseStream, p.StandardInput.BaseStream)
                .RegisterIncomingMessageType<RpcLogEntry>()
                .RegisterIncomingMessageType<RpcSyncResponse>()
                .RegisterIncomingMessageType<RpcSecurityMechanismRequest>()
                .RegisterIncomingMessageType<RpcTanRequest>()
                .RegisterIncomingMessageType<RpcDone>()
                .Connect(ct);

            await rpc.Send(new RpcSyncRequest(
                    AccountId: "id" + connectionId,
                    HbciVersion: hbciVersion,
                    Blz: bankCode,
                    User: userId,
                    CustomerId: customerId,
                    Pin: pin,
                    StartDate: null),
                ct);

            var response = await HandleMessages(rpc, callbackHandler, ct);

            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0)
                throw new Exception("Adapter exit code: " + p.ExitCode);

            if (response == null)
                throw new Exception("Adapter did not report a result");

            return response;
        }
        finally
        {
            await KillAdapter(p);
        }
    }

    private async Task KillAdapter(Process p)
    {
        if (p.HasExited)
            return;

        logger.LogWarning("HBCI adapter will be killed because the import was canceled but the adapter is still running.");
        p.Kill(true);
        var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await p.WaitForExitAsync(timeout.Token);
            logger.LogWarning("HBCI adapter was killed.");
        }
        catch (OperationCanceledException)
        {
            throw new Exception("HBCI Adapter was supposed to be killed because the import process was canceled but it is still running after 30 seconds.");
        }
    }

    private static async Task<RpcSyncResponse?> HandleMessages(RpcBridge rpc, IAdapterCallbackHandler callbackHandler, CancellationToken ct)
    {
        RpcSyncResponse? response = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var message = await rpc.Read(ct);
            switch (message)
            {
                case RpcLogEntry logEntry:
                    await callbackHandler.OnLogMessage(logEntry.Severity, logEntry.Message);
                    break;

                case RpcTanRequest tanRequest:
                    var tan = await callbackHandler.OnTanRequired(tanRequest.Message);
                    if (tan == null)
                        throw new OperationCanceledException("No tan was provided.");
                    await rpc.Send(new RpcTanResponse(tan), ct);
                    break;

                case RpcSecurityMechanismRequest securityMechanismRequest:
                    var mapped = securityMechanismRequest.Entries.ToImmutableDictionary(x => x.Code, x => x.Name);
                    var code = await callbackHandler.OnSecurityMechanismRequired(mapped);
                    if (code == null)
                        throw new OperationCanceledException("No sec mechanism was provided.");
                    await rpc.Send(new RpcSecurityMechanismResponse(code), ct);
                    break;

                case RpcSyncResponse syncResponse:
                    response = syncResponse;
                    break;

                case RpcDone:
                    return response;

                default:
                    throw new NotSupportedException("Unknown messages received");
            }
        }
    }
}

public record HbciAdapterOptions
{
    public required string WorkingDirectory { get; set; }
    public required string FileName { get; set; }
    public required string Arguments { get; set; }
}

public interface IAdapterCallbackHandler
{
    Task<string?> OnTanRequired(string message);
    Task<string?> OnSecurityMechanismRequired(ImmutableDictionary<string, string> securityMechanism);
    Task OnLogMessage(int severity, string message);
}