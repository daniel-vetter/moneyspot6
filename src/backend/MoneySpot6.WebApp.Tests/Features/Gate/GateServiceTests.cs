using MoneySpot6.WebApp.Features.Core.Gate;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.Gate;

[TestFixture]
public class GateServiceTests
{
    [Test]
    public void InterpretResponse_BlockedTrueWithMessage_Blocks()
    {
        var result = GateService.InterpretResponse(200, """{ "blocked": true, "message": "Wartung" }""");

        result.Blocked.ShouldBeTrue();
        result.Message.ShouldBe("Wartung");
    }

    [Test]
    public void InterpretResponse_BlockedTrueWithoutMessage_BlocksWithNullMessage()
    {
        var result = GateService.InterpretResponse(200, """{ "blocked": true }""");

        result.Blocked.ShouldBeTrue();
        result.Message.ShouldBeNull();
    }

    [Test]
    public void InterpretResponse_BlockedFalse_Allows()
    {
        var result = GateService.InterpretResponse(200, """{ "blocked": false, "message": "ignored" }""");

        result.Blocked.ShouldBeFalse();
    }

    [Test]
    public void InterpretResponse_BlockedFieldMissing_Allows()
    {
        var result = GateService.InterpretResponse(200, """{ "something": "else" }""");

        result.Blocked.ShouldBeFalse();
    }

    [Test]
    public void InterpretResponse_InvalidJson_Allows()
    {
        var result = GateService.InterpretResponse(200, "not json at all");

        result.Blocked.ShouldBeFalse();
    }

    [Test]
    public void InterpretResponse_EmptyBody_Allows()
    {
        GateService.InterpretResponse(200, "").Blocked.ShouldBeFalse();
        GateService.InterpretResponse(200, null).Blocked.ShouldBeFalse();
    }

    [Test]
    public void InterpretResponse_NonSuccessStatus_Allows()
    {
        // Even a body that says blocked:true must be ignored on a non-2xx response.
        GateService.InterpretResponse(500, """{ "blocked": true, "message": "x" }""").Blocked.ShouldBeFalse();
        GateService.InterpretResponse(404, """{ "blocked": true }""").Blocked.ShouldBeFalse();
        GateService.InterpretResponse(302, """{ "blocked": true }""").Blocked.ShouldBeFalse();
    }
}
