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
    public string? CurrentDigest { get; set; }
    public string? RemoteDigest { get; set; }
    public List<string> PulledImages { get; } = [];
    public RunContainerRequest? LastRunContainerRequest { get; private set; }

    public FakeDockerService(
        string imageReference,
        string containerName = "test-container",
        string imageId = "sha256:abc123",
        string? currentDigest = null,
        ImmutableArray<PortBindingConfig>? ports = null,
        ImmutableArray<string>? binds = null,
        ImmutableArray<string>? env = null,
        string? restartPolicy = null,
        string? networkMode = null)
    {
        _imageReference = imageReference;
        _containerName = containerName;
        _imageId = imageId;
        CurrentDigest = currentDigest;
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
            ImageInfo.Parse(_imageReference),
            _imageId,
            _ports,
            _binds,
            _env,
            _restartPolicy,
            _networkMode));
    }

    public Task<string?> GetImageDigest(string imageId)
    {
        return Task.FromResult<string?>(CurrentDigest ?? $"{_imageReference.Split(':')[0]}@sha256:currentdigest");
    }

    public Task<string?> GetRemoteDigest(ImageInfo imageInfo)
    {
        return Task.FromResult(RemoteDigest ?? $"{imageInfo.ImageWithoutTag}@sha256:remotedigest");
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
