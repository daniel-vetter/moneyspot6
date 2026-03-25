using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class UpdateExecutorTests
{
    [Test]
    public async Task Pulls_sidecar_and_app_image()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest");
        var sut = CreateExecutor(fake);

        await sut.Execute();

        fake.PulledImages.ShouldContain("docker:cli");
        fake.PulledImages.ShouldContain("ghcr.io/daniel-vetter/moneyspot6:latest");
    }

    [Test]
    public async Task Container_request_has_auto_remove_and_docker_socket()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest");
        var sut = CreateExecutor(fake);

        await sut.Execute();

        fake.LastRunContainerRequest.ShouldNotBeNull();
        fake.LastRunContainerRequest.AutoRemove.ShouldBeTrue();
        fake.LastRunContainerRequest.Binds.ShouldContain("/var/run/docker.sock:/var/run/docker.sock");
        fake.LastRunContainerRequest.Image.ShouldBe("docker:cli");
    }

    [Test]
    public async Task Script_contains_stop_rm_and_run()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest", containerName: "moneyspot");
        var sut = CreateExecutor(fake);

        await sut.Execute();

        var script = GetScript(fake);
        script.ShouldContain("docker stop moneyspot");
        script.ShouldContain("docker rm moneyspot");
        script.ShouldContain("docker run -d --name moneyspot");
        script.ShouldContain("ghcr.io/daniel-vetter/moneyspot6:latest");
    }

    [Test]
    public async Task Script_includes_port_bindings()
    {
        var fake = new FakeDockerService("registry.example.com/myapp:v2", containerName: "myapp",
            ports: [new PortBindingConfig("8080/tcp", "80", null)]);
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("-p 80:8080/tcp");
    }

    [Test]
    public async Task Script_includes_port_binding_with_host_ip()
    {
        var fake = new FakeDockerService("app:latest",
            ports: [new PortBindingConfig("443/tcp", "8443", "127.0.0.1")]);
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("-p 127.0.0.1:8443:443/tcp");
    }

    [Test]
    public async Task Script_includes_volume_binds()
    {
        var fake = new FakeDockerService("app:latest",
            binds: ["/host/data:/container/data", "/var/run/docker.sock:/var/run/docker.sock"]);
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("-v /host/data:/container/data");
        script.ShouldContain("-v /var/run/docker.sock:/var/run/docker.sock");
    }

    [Test]
    public async Task Script_includes_environment_variables()
    {
        var fake = new FakeDockerService("app:latest",
            env: ["FOO=bar", "DB_CONNECTION=host=localhost;port=5432"]);
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("-e 'FOO=bar'");
        script.ShouldContain("-e 'DB_CONNECTION=host=localhost;port=5432'");
    }

    [Test]
    public async Task Script_includes_restart_policy()
    {
        var fake = new FakeDockerService("app:latest", restartPolicy: "unless-stopped");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("--restart unless-stopped");
    }

    [Test]
    public async Task Script_excludes_no_restart_policy()
    {
        var fake = new FakeDockerService("app:latest", restartPolicy: "no");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldNotContain("--restart");
    }

    [Test]
    public async Task Script_includes_custom_network()
    {
        var fake = new FakeDockerService("app:latest", networkMode: "traefik");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("--network traefik");
    }

    [Test]
    public async Task Script_excludes_default_network()
    {
        var fake = new FakeDockerService("app:latest", networkMode: "default");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldNotContain("--network");
    }

    [Test]
    public async Task Script_excludes_bridge_network()
    {
        var fake = new FakeDockerService("app:latest", networkMode: "bridge");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldNotContain("--network");
    }

    [Test]
    public async Task Uses_detected_image_not_hardcoded()
    {
        var fake = new FakeDockerService("my.private.registry:5000/team/app:stable");
        var script = await ExecuteAndGetScript(fake);

        fake.PulledImages.ShouldContain("my.private.registry:5000/team/app:stable");
        script.ShouldContain("my.private.registry:5000/team/app:stable");
    }

    [Test]
    public async Task Script_with_full_config()
    {
        var fake = new FakeDockerService("ghcr.io/user/app:latest", containerName: "myapp",
            ports: [new PortBindingConfig("8080/tcp", "80", null)],
            binds: ["/data:/app/data"],
            env: ["ASPNETCORE_ENVIRONMENT=Production"],
            restartPolicy: "always",
            networkMode: "traefik");
        var script = await ExecuteAndGetScript(fake);

        script.ShouldContain("docker stop myapp");
        script.ShouldContain("docker rm myapp");
        script.ShouldContain("-p 80:8080/tcp");
        script.ShouldContain("-v /data:/app/data");
        script.ShouldContain("-e 'ASPNETCORE_ENVIRONMENT=Production'");
        script.ShouldContain("--restart always");
        script.ShouldContain("--network traefik");
        script.ShouldContain("ghcr.io/user/app:latest");
    }

    private static string GetScript(FakeDockerService fake)
    {
        fake.LastRunContainerRequest.ShouldNotBeNull();
        fake.LastRunContainerRequest.Cmd.Length.ShouldBe(3);
        return fake.LastRunContainerRequest.Cmd[2];
    }

    private static async Task<string> ExecuteAndGetScript(FakeDockerService fake)
    {
        var sut = CreateExecutor(fake);
        await sut.Execute();
        return GetScript(fake);
    }

    private static UpdateExecutor CreateExecutor(IDockerService dockerService)
    {
        return new UpdateExecutor(
            NullLogger<UpdateExecutor>.Instance,
            dockerService);
    }
}
