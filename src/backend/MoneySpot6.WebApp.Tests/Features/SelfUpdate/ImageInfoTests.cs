using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class ImageInfoTests
{
    [Test]
    public void Ghcr_with_tag()
    {
        var result = ImageInfo.Parse("ghcr.io/daniel-vetter/moneyspot6:latest");

        result.FullReference.ShouldBe("ghcr.io/daniel-vetter/moneyspot6:latest");
        result.ImageWithoutTag.ShouldBe("ghcr.io/daniel-vetter/moneyspot6");
        result.Tag.ShouldBe("latest");
        result.RegistryHost.ShouldBe("ghcr.io");
        result.ImagePath.ShouldBe("daniel-vetter/moneyspot6");
    }

    [Test]
    public void Ghcr_with_specific_tag()
    {
        var result = ImageInfo.Parse("ghcr.io/daniel-vetter/moneyspot6:v1.2.3");

        result.Tag.ShouldBe("v1.2.3");
        result.ImageWithoutTag.ShouldBe("ghcr.io/daniel-vetter/moneyspot6");
        result.RegistryHost.ShouldBe("ghcr.io");
    }

    [Test]
    public void Ghcr_without_tag_defaults_to_latest()
    {
        var result = ImageInfo.Parse("ghcr.io/daniel-vetter/moneyspot6");

        result.Tag.ShouldBe("latest");
        result.ImageWithoutTag.ShouldBe("ghcr.io/daniel-vetter/moneyspot6");
        result.RegistryHost.ShouldBe("ghcr.io");
    }

    [Test]
    public void Docker_hub_with_namespace()
    {
        var result = ImageInfo.Parse("myuser/myapp:stable");

        result.Tag.ShouldBe("stable");
        result.RegistryHost.ShouldBe("registry-1.docker.io");
        result.ImagePath.ShouldBe("myuser/myapp");
    }

    [Test]
    public void Docker_hub_official_image()
    {
        var result = ImageInfo.Parse("nginx:alpine");

        result.Tag.ShouldBe("alpine");
        result.RegistryHost.ShouldBe("registry-1.docker.io");
        result.ImagePath.ShouldBe("library/nginx");
    }

    [Test]
    public void Custom_registry_with_port()
    {
        var result = ImageInfo.Parse("myregistry.local:5000/team/app:v2");

        result.Tag.ShouldBe("v2");
        result.RegistryHost.ShouldBe("myregistry.local:5000");
        result.ImagePath.ShouldBe("team/app");
    }

    [Test]
    public void Custom_registry_without_tag()
    {
        var result = ImageInfo.Parse("registry.example.com/moneyspot6");

        result.Tag.ShouldBe("latest");
        result.RegistryHost.ShouldBe("registry.example.com");
        result.ImagePath.ShouldBe("moneyspot6");
    }
}
