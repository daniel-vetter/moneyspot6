using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public static class TestExtensions
{
    public static T ShouldBeOkObjectResult<T>(this IActionResult result)
    {
        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.ShouldBeOfType<T>();
        return (T)okResult.Value!;
    }

    public static T ShouldBeOkObjectResult<T>(this ActionResult<T> result)
    {
        result.Result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.ShouldBeOfType<T>();
        return (T)okResult.Value!;
    }

    public static T ShouldBeBadRequestObjectResult<T>(this IActionResult result)
    {
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badResult = (BadRequestObjectResult)result;
        badResult.Value.ShouldBeOfType<T>();
        return (T)badResult.Value!;
    }
}
