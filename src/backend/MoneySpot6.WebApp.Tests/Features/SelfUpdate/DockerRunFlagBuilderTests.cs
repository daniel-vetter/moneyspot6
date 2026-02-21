using System.Collections.Immutable;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class DockerRunFlagBuilderTests
{
    private readonly DockerRunFlagBuilder _sut = new();

    private static ContainerConfig Config(
        ImmutableArray<PortBindingConfig>? ports = null,
        ImmutableArray<string>? binds = null,
        ImmutableArray<string>? env = null,
        string? restartPolicy = null,
        string? networkMode = null)
    {
        return new ContainerConfig(
            ports ?? [],
            binds ?? [],
            env ?? [],
            restartPolicy,
            networkMode);
    }

    [Test]
    public void Empty_config_returns_empty_flags()
    {
        _sut.BuildRunFlags(Config()).ShouldBe("");
    }

    [Test]
    public void Port_binding_without_host_ip()
    {
        var config = Config(ports: [new PortBindingConfig("8080/tcp", "80", null)]);
        _sut.BuildRunFlags(config).ShouldBe("-p 80:8080/tcp");
    }

    [Test]
    public void Port_binding_with_host_ip()
    {
        var config = Config(ports: [new PortBindingConfig("443/tcp", "8443", "127.0.0.1")]);
        _sut.BuildRunFlags(config).ShouldBe("-p 127.0.0.1:8443:443/tcp");
    }

    [Test]
    public void Multiple_port_bindings()
    {
        var config = Config(ports:
        [
            new PortBindingConfig("80/tcp", "80", null),
            new PortBindingConfig("443/tcp", "443", null)
        ]);

        var result = _sut.BuildRunFlags(config);
        result.ShouldContain("-p 80:80/tcp");
        result.ShouldContain("-p 443:443/tcp");
    }

    [Test]
    public void Multiple_bindings_for_same_container_port()
    {
        var config = Config(ports:
        [
            new PortBindingConfig("8080/tcp", "80", null),
            new PortBindingConfig("8080/tcp", "8080", "0.0.0.0")
        ]);

        var result = _sut.BuildRunFlags(config);
        result.ShouldContain("-p 80:8080/tcp");
        result.ShouldContain("-p 0.0.0.0:8080:8080/tcp");
    }

    [Test]
    public void Volume_binds()
    {
        var config = Config(binds: ["/host/data:/container/data", "/var/run/docker.sock:/var/run/docker.sock"]);

        var result = _sut.BuildRunFlags(config);
        result.ShouldContain("-v /host/data:/container/data");
        result.ShouldContain("-v /var/run/docker.sock:/var/run/docker.sock");
    }

    [Test]
    public void Environment_variables()
    {
        var config = Config(env: ["FOO=bar", "DB_CONNECTION=host=localhost;port=5432"]);

        var result = _sut.BuildRunFlags(config);
        result.ShouldContain("-e 'FOO=bar'");
        result.ShouldContain("-e 'DB_CONNECTION=host=localhost;port=5432'");
    }

    [Test]
    public void Restart_policy_always()
    {
        var config = Config(restartPolicy: "always");
        _sut.BuildRunFlags(config).ShouldContain("--restart always");
    }

    [Test]
    public void Restart_policy_unless_stopped()
    {
        var config = Config(restartPolicy: "unless-stopped");
        _sut.BuildRunFlags(config).ShouldContain("--restart unless-stopped");
    }

    [Test]
    public void Restart_policy_on_failure_with_max_retry()
    {
        var config = Config(restartPolicy: "on-failure:5");
        _sut.BuildRunFlags(config).ShouldContain("--restart on-failure:5");
    }

    [Test]
    public void Restart_policy_no_is_not_included()
    {
        var config = Config(restartPolicy: "no");
        _sut.BuildRunFlags(config).ShouldNotContain("--restart");
    }

    [Test]
    public void Restart_policy_null_is_not_included()
    {
        var config = Config(restartPolicy: null);
        _sut.BuildRunFlags(config).ShouldNotContain("--restart");
    }

    [Test]
    public void Restart_policy_empty_is_not_included()
    {
        var config = Config(restartPolicy: "");
        _sut.BuildRunFlags(config).ShouldNotContain("--restart");
    }

    [Test]
    public void Custom_network()
    {
        var config = Config(networkMode: "my-network");
        _sut.BuildRunFlags(config).ShouldBe("--network my-network");
    }

    [Test]
    public void Default_network_is_not_included()
    {
        var config = Config(networkMode: "default");
        _sut.BuildRunFlags(config).ShouldNotContain("--network");
    }

    [Test]
    public void Bridge_network_is_not_included()
    {
        var config = Config(networkMode: "bridge");
        _sut.BuildRunFlags(config).ShouldNotContain("--network");
    }

    [Test]
    public void Null_network_is_not_included()
    {
        var config = Config(networkMode: null);
        _sut.BuildRunFlags(config).ShouldNotContain("--network");
    }

    [Test]
    public void Full_container_config()
    {
        var config = Config(
            ports: [new PortBindingConfig("8080/tcp", "80", null)],
            binds: ["/data:/app/data"],
            env: ["ASPNETCORE_ENVIRONMENT=Production"],
            restartPolicy: "always",
            networkMode: "traefik");

        var result = _sut.BuildRunFlags(config);
        result.ShouldContain("-p 80:8080/tcp");
        result.ShouldContain("-v /data:/app/data");
        result.ShouldContain("-e 'ASPNETCORE_ENVIRONMENT=Production'");
        result.ShouldContain("--restart always");
        result.ShouldContain("--network traefik");
    }

    [Test]
    public void Flags_appear_in_correct_order()
    {
        var config = Config(
            ports: [new PortBindingConfig("80/tcp", "80", null)],
            binds: ["/data:/data"],
            env: ["X=1"],
            restartPolicy: "always",
            networkMode: "custom");

        var result = _sut.BuildRunFlags(config);
        var pIdx = result.IndexOf("-p ");
        var vIdx = result.IndexOf("-v ");
        var eIdx = result.IndexOf("-e ");
        var rIdx = result.IndexOf("--restart ");
        var nIdx = result.IndexOf("--network ");

        pIdx.ShouldBeLessThan(vIdx);
        vIdx.ShouldBeLessThan(eIdx);
        eIdx.ShouldBeLessThan(rIdx);
        rIdx.ShouldBeLessThan(nIdx);
    }
}
