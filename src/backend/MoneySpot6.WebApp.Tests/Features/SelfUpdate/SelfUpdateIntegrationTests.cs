using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class SelfUpdateIntegrationTests
{
    private DockerClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = new DockerClientConfiguration().CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    public async Task Full_update_flow_with_real_container()
    {
        var containerName = $"selfupdate-test-{Guid.NewGuid():N}";

        await PullImage("alpine:3.20");

        var created = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "alpine:3.20",
            Name = containerName,
            Cmd = ["sleep", "300"],
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["8080/tcp"] = [new PortBinding { HostPort = "0" }]
                },
                Binds = ["/tmp/selfupdate-test:/data:ro"],
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No }
            },
            Env = ["TEST_VAR=hello"]
        });

        try
        {
            await _client.Containers.StartContainerAsync(created.ID, new ContainerStartParameters());

            var dockerService = new DockerService(NullLogger<DockerService>.Instance);

            // 1. InspectContainer should return correct data
            var inspection = await dockerService.InspectContainer(created.ID);

            inspection.ContainerName.ShouldBe(containerName);
            inspection.ImageReference.ShouldBe("alpine:3.20");
            inspection.Env.ShouldContain("TEST_VAR=hello");
            inspection.Binds.ShouldContain("/tmp/selfupdate-test:/data:ro");
            inspection.PortBindings.ShouldContain(p => p.ContainerPort == "8080/tcp");

            // 2. GetImageId should return an ID for the image reference
            var imageId = await dockerService.GetImageId("alpine:3.20");
            imageId.ShouldStartWith("sha256:");

            // 3. BuildScript should produce a valid script from the inspection
            var executor = new UpdateExecutor(
                NullLogger<UpdateExecutor>.Instance,
                dockerService);
            var script = executor.BuildScript(inspection);

            script.ShouldContain($"docker stop {containerName}");
            script.ShouldContain($"docker rm {containerName}");
            script.ShouldContain("alpine:3.20");
            script.ShouldContain("-v /tmp/selfupdate-test:/data:ro");
            script.ShouldContain("-e 'TEST_VAR=hello'");
            script.ShouldContain("-p ");
        }
        finally
        {
            try { await _client.Containers.StopContainerAsync(containerName, new ContainerStopParameters()); }
            catch { /* ignore */ }
            try { await _client.Containers.RemoveContainerAsync(containerName, new ContainerRemoveParameters { Force = true }); }
            catch { /* ignore */ }
        }
    }

    private async Task PullImage(string image)
    {
        var parts = image.Split(':');
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = parts[0], Tag = parts.Length > 1 ? parts[1] : "latest" },
            null,
            new Progress<JSONMessage>());
    }
}
