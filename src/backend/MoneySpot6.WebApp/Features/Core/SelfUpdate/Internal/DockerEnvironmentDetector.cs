namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService]
public class DockerEnvironmentDetector
{
    public bool IsDockerWithSocket { get; }

    public DockerEnvironmentDetector()
    {
        IsDockerWithSocket = File.Exists("/.dockerenv") && File.Exists("/var/run/docker.sock");
    }
}
