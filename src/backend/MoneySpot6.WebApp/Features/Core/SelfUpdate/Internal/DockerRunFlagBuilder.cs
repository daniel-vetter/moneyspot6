using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService]
public class DockerRunFlagBuilder
{
    public string BuildRunFlags(ContainerConfig config)
    {
        var flags = new List<string>();

        foreach (var port in config.PortBindings)
        {
            var hostPart = string.IsNullOrEmpty(port.HostIp) ? port.HostPort : $"{port.HostIp}:{port.HostPort}";
            flags.Add($"-p {hostPart}:{port.ContainerPort}");
        }

        foreach (var bind in config.Binds)
        {
            flags.Add($"-v {bind}");
        }

        foreach (var env in config.Env)
        {
            flags.Add($"-e '{env}'");
        }

        if (config.RestartPolicy is { } restart && restart != "" && restart != "no")
        {
            flags.Add($"--restart {restart}");
        }

        if (config.NetworkMode is { } network && network != "" && network != "default" && network != "bridge")
        {
            flags.Add($"--network {network}");
        }

        return string.Join(" ", flags);
    }
}

public record ContainerConfig(
    ImmutableArray<PortBindingConfig> PortBindings,
    ImmutableArray<string> Binds,
    ImmutableArray<string> Env,
    string? RestartPolicy,
    string? NetworkMode);

public record PortBindingConfig(string ContainerPort, string HostPort, string? HostIp);
