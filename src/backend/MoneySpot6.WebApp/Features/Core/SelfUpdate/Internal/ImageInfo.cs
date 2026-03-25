namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

public record ImageInfo(
    string FullReference,
    string ImageWithoutTag,
    string Tag,
    string RegistryHost,
    string ImagePath)
{
    public static ImageInfo Parse(string imageReference)
    {
        var tagSeparator = imageReference.LastIndexOf(':');
        string imageWithoutTag;
        string tag;

        if (tagSeparator > 0 && !imageReference[(tagSeparator + 1)..].Contains('/'))
        {
            imageWithoutTag = imageReference[..tagSeparator];
            tag = imageReference[(tagSeparator + 1)..];
        }
        else
        {
            imageWithoutTag = imageReference;
            tag = "latest";
        }

        var firstSlash = imageWithoutTag.IndexOf('/');
        string registryHost;
        string imagePath;

        if (firstSlash > 0 && (imageWithoutTag[..firstSlash].Contains('.') || imageWithoutTag[..firstSlash].Contains(':')))
        {
            registryHost = imageWithoutTag[..firstSlash];
            imagePath = imageWithoutTag[(firstSlash + 1)..];
        }
        else
        {
            registryHost = "registry-1.docker.io";
            imagePath = firstSlash > 0 ? imageWithoutTag : $"library/{imageWithoutTag}";
        }

        return new ImageInfo(imageReference, imageWithoutTag, tag, registryHost, imagePath);
    }
}
