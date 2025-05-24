using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

[ScopedService]
public class ExternalDataProvider(IOptions<HbciAdapterOptions> options, ILogger<ExternalDataProvider> logger, ExternalProcessMonitor externalProcessMonitor)
{
    public async Task<RpcSyncResponse> Run(int connectionId, string hbciVersion, string bankCode, string userId, string customerId, string pin, DateTimeOffset? startDate, IAdapterCallbackHandler callbackHandler, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var p = new Process();
        p.StartInfo = new ProcessStartInfo();
        p.StartInfo.FileName = ResolveSolutionDir(options.Value.FileName);
        p.StartInfo.Arguments = ResolveSolutionDir(options.Value.Arguments);
        p.StartInfo.WorkingDirectory = ResolveSolutionDir(options.Value.WorkingDirectory);
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        if (!p.Start())
            throw new Exception("Could not start adapter");

        externalProcessMonitor.AddProcessId(p.Id);

        try
        {
            var rpc = await new RpcBridge(p.StandardOutput.BaseStream, p.StandardInput.BaseStream)
                .RegisterIncomingMessageType<RpcLogEntry>()
                .RegisterIncomingMessageType<RpcSyncResponse>()
                .RegisterIncomingMessageType<RpcSecurityMechanismRequest>()
                .RegisterIncomingMessageType<RpcTanRequest>()
                .RegisterIncomingMessageType<RpcDone>()
                .RegisterIncomingMessageType<RpcException>()
                .Connect(ct);

            await rpc.Send(new RpcSyncRequest(
                    AccountId: "id" + connectionId,
                    HbciVersion: hbciVersion,
                    BankCode: bankCode,
                    UserId: userId,
                    CustomerId: customerId,
                    Pin: pin,
                    StartDate: startDate?.ToString("u")),
                ct);

            var response = await HandleMessages(rpc, callbackHandler, ct);

            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0)
                throw new Exception("Adapter exit code: " + p.ExitCode);
            
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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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

    private string ResolveSolutionDir(string str)
    {
        const string placeholder = "${solutionDir}";
        if (!str.Contains(placeholder))
            return str;

        var curDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

        while (true)
        {
            if (Directory.Exists(Path.Combine(curDir, ".git")))
                break;

            curDir = Path.GetFullPath(Path.Combine(curDir, ".."));
        }

        return str.Replace(placeholder, curDir);
    }

    private static async Task<RpcSyncResponse> HandleMessages(RpcBridge rpc, IAdapterCallbackHandler callbackHandler, CancellationToken ct)
    {
        RpcSyncResponse? response = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var message = await rpc.Read(ct);
            switch (message)
            {
                case RpcLogEntry logEntry:
                    await callbackHandler.OnLogMessage(logEntry.Severity, logEntry.Message, ct);
                    break;

                case RpcTanRequest tanRequest:
                    var tan = await callbackHandler.OnTanRequired(tanRequest.Message, ct);
                    if (tan == null)
                        throw new CanceledByUserException();
                    await rpc.Send(new RpcTanResponse(tan), ct);
                    break;

                case RpcSecurityMechanismRequest securityMechanismRequest:
                    var mapped = securityMechanismRequest.Entries.ToImmutableDictionary(x => x.Code, x => x.Name);
                    var code = await callbackHandler.OnSecurityMechanismRequired(mapped, ct);
                    if (code == null)
                        throw new CanceledByUserException();
                    await rpc.Send(new RpcSecurityMechanismResponse(code), ct);
                    break;

                case RpcException exception:
                    throw new Exception("Adapter reported a exception: " + exception.Message);

                case RpcSyncResponse syncResponse:
                    response = syncResponse;
                    break;

                case RpcDone:
                    if (response == null)
                        throw new Exception("Adapter did not return a result");
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
    Task<string?> OnTanRequired(string message, CancellationToken ct);
    Task<string?> OnSecurityMechanismRequired(ImmutableDictionary<string, string> securityMechanism, CancellationToken ct);
    Task OnLogMessage(int severity, string message, CancellationToken ct);
}

public class CanceledByUserException : Exception
{

}