using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Tests.Api;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.Config;

public class KeyValueConfigurationTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    private record SampleConfig(string Name, int Count, bool Flag);

    [Test]
    public async Task Get_MissingKey_ReturnsDefault()
    {
        var config = Get<KeyValueConfiguration>();

        var value = await config.Get("does-not-exist", 42);

        value.ShouldBe(42);
    }

    [Test]
    public async Task SetGet_Bool_Roundtrips()
    {
        var config = Get<KeyValueConfiguration>();

        await config.Set("flag", true);

        (await config.Get("flag", false)).ShouldBe(true);
    }

    [Test]
    public async Task SetGet_Int_Roundtrips()
    {
        var config = Get<KeyValueConfiguration>();

        await config.Set("count", 1234);

        (await config.Get("count", 0)).ShouldBe(1234);
    }

    [Test]
    public async Task SetGet_Decimal_RoundtripsWithInvariantCulture()
    {
        var config = Get<KeyValueConfiguration>();

        await config.Set("amount", 1234.56m);

        (await config.Get("amount", 0m)).ShouldBe(1234.56m);
    }

    [Test]
    public async Task SetGet_String_Roundtrips()
    {
        var config = Get<KeyValueConfiguration>();

        await config.Set("name", "Hello, World!");

        (await config.Get("name", "")).ShouldBe("Hello, World!");
    }

    [Test]
    public async Task SetGet_DateTime_PreservesUtcKindAndPrecision()
    {
        var config = Get<KeyValueConfiguration>();
        var original = new DateTime(2024, 6, 15, 12, 34, 56, 789, DateTimeKind.Utc);

        await config.Set("ts", original);

        var roundtripped = await config.Get("ts", DateTime.MinValue);
        roundtripped.ShouldBe(original);
        roundtripped.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public async Task SetGet_DateTimeOffset_PreservesOffset()
    {
        var config = Get<KeyValueConfiguration>();
        var original = new DateTimeOffset(2024, 6, 15, 12, 34, 56, 789, TimeSpan.FromHours(5));

        await config.Set("ts", original);

        var roundtripped = await config.Get("ts", DateTimeOffset.MinValue);
        roundtripped.ShouldBe(original);
        roundtripped.Offset.ShouldBe(TimeSpan.FromHours(5));
    }

    [Test]
    public async Task SetGet_ComplexType_UsesJson()
    {
        var config = Get<KeyValueConfiguration>();
        var original = new SampleConfig("Test", 42, true);

        await config.Set("complex", original);

        (await config.Get<SampleConfig?>("complex", null)).ShouldBe(original);
    }

    [Test]
    public async Task Set_OverwritesExistingValue()
    {
        var config = Get<KeyValueConfiguration>();

        await config.Set("count", 1);
        await config.Set("count", 99);

        (await config.Get("count", 0)).ShouldBe(99);
    }

    [Test]
    public async Task Get_WithoutDefault_ReturnsStoredValue()
    {
        var config = Get<KeyValueConfiguration>();
        await config.Set("flag", true);

        (await config.Get<bool>("flag")).ShouldBe(true);
    }

    [Test]
    public async Task Get_WithoutDefault_MissingKey_Throws()
    {
        var config = Get<KeyValueConfiguration>();

        await Should.ThrowAsync<InvalidOperationException>(() => config.Get<bool>("missing"));
    }

    [Test]
    public async Task Get_WithMismatchedType_Throws()
    {
        var config = Get<KeyValueConfiguration>();
        await config.Set("flag", true);

        await Should.ThrowAsync<InvalidOperationException>(() => config.Get("flag", 0));
    }

    [Test]
    public async Task Get_WithoutDefault_MismatchedType_Throws()
    {
        var config = Get<KeyValueConfiguration>();
        await config.Set("flag", true);

        await Should.ThrowAsync<InvalidOperationException>(() => config.Get<int>("flag"));
    }

    [Test]
    public async Task Set_NullValue_Throws()
    {
        var config = Get<KeyValueConfiguration>();

        await Should.ThrowAsync<ArgumentNullException>(() => config.Set<string?>("name", null));
    }

    [Test]
    public async Task Set_DifferentType_Throws()
    {
        var config = Get<KeyValueConfiguration>();
        await config.Set("k", true);

        await Should.ThrowAsync<InvalidOperationException>(() => config.Set("k", 42));
    }

    [Test]
    public async Task Set_DifferentType_DoesNotOverwriteOriginalValue()
    {
        var config = Get<KeyValueConfiguration>();
        await config.Set("k", true);

        try { await config.Set("k", 42); } catch (InvalidOperationException) { }

        (await config.Get<bool>("k")).ShouldBe(true);
    }
}
