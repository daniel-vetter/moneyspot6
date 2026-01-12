using System.Collections.Immutable;
using System.Diagnostics;

namespace MoneySpot6.WebApp.Features.Core.AccountSync.FinTs.Adapter;

[SingletonService]
public class ExternalProcessMonitor
{
    List<int> _ids = new();

    public void AddProcessId(int processId)
    {
        lock(_ids)
            _ids.Add(processId);
    }

    public ImmutableArray<RunningProcess> GetRunningAdapters()
    {
        int[] ids;

        lock (_ids)
            ids = _ids.ToArray();

        var r = ImmutableArray.CreateBuilder<RunningProcess>();
        foreach (var id in ids)
        {
            var entry = new RunningProcess(id, null, null);

            try
            {
                var p = Process.GetProcessById(id);
                entry = entry with
                {
                    StartTime = p.StartTime
                };
            }
            catch (Exception e)
            {
                entry = entry with
                {
                    Error = e.Message
                };
            }

            r.Add(entry);
        }
        return r.ToImmutableArray();
    }
}

public record RunningProcess(int Id, DateTime? StartTime, string? Error);