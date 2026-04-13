using System.Collections.Immutable;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class FakeDockerService : IDockerService
{
    private readonly string _imageReference;
    private readonly string _containerName;
    private readonly string _imageId;
    private readonly ImmutableArray<PortBindingConfig> _ports;
    private readonly ImmutableArray<string> _binds;
    private readonly ImmutableArray<string> _env;
    private readonly string? _restartPolicy;
    private readonly string? _networkMode;

    public bool IsRunningInContainer { get; set; } = true;
    public bool IsDockerSocketAvailable { get; set; } = true;
    public string LatestImageId { get; set; } = "sha256:latest456";
    public List<string> PulledImages { get; } = [];
    public RunContainerRequest? LastRunContainerRequest { get; private set; }

    public FakeDockerService(
        string imageReference,
        string containerName = "test-container",
        string imageId = "sha256:abc123",
        ImmutableArray<PortBindingConfig>? ports = null,
        ImmutableArray<string>? binds = null,
        ImmutableArray<string>? env = null,
        string? restartPolicy = null,
        string? networkMode = null)
    {
        _imageReference = imageReference;
        _containerName = containerName;
        _imageId = imageId;
        _ports = ports ?? [];
        _binds = binds ?? [];
        _env = env ?? [];
        _restartPolicy = restartPolicy;
        _networkMode = networkMode;
    }

    public Task<ContainerInspection> InspectContainer(string containerId)
    {
        return Task.FromResult(new ContainerInspection(
            containerId,
            _containerName,
            _imageReference,
            _imageId,
            _ports,
            _binds,
            _env,
            _restartPolicy,
            _networkMode));
    }

    public Task<string> GetImageId(string imageReference)
    {
        return Task.FromResult(LatestImageId);
    }

    public Task PullImage(string image)
    {
        PulledImages.Add(image);
        return Task.CompletedTask;
    }

    public Task<string> RunContainer(RunContainerRequest request)
    {
        LastRunContainerRequest = request;
        return Task.FromResult("container-id");
    }
}
