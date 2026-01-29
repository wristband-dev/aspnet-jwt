using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class RequireWristbandJwtTests
{
    [Fact]
    public void RequireWristbandJwt_RouteHandlerBuilder_CallsRequireAuthorizationWithCorrectPolicy()
    {
        var services = new ServiceCollection();
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var routeHandlerBuilder = app.MapGet("/test", () => "test");

        var result = routeHandlerBuilder.RequireWristbandJwt();
        Assert.NotNull(result);
        Assert.Same(routeHandlerBuilder, result); // Should return the same builder for chaining
    }

    [Fact]
    public void RequireWristbandJwt_RouteHandlerBuilder_ExtensionMethodExists()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(RouteHandlerBuilder) }, null);

        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
        Assert.True(method.IsPublic);
        Assert.Equal(typeof(RouteHandlerBuilder), method.ReturnType);
    }

    [Fact]
    public void RequireWristbandJwt_RouteHandlerBuilder_HasCorrectParameterName()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", new[] { typeof(RouteHandlerBuilder) });

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("builder", parameters[0].Name);
    }

    [Fact]
    public void RequireWristbandJwt_RouteHandlerBuilder_IsExtensionMethod()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", new[] { typeof(RouteHandlerBuilder) });

        Assert.NotNull(method);
        Assert.True(method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
    }

    [Fact]
    public void RequireWristbandJwt_RouteGroupBuilder_CallsRequireAuthorizationWithCorrectPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var routeGroupBuilder = app.MapGroup("/api");

        var result = routeGroupBuilder.RequireWristbandJwt();
        Assert.NotNull(result);
        Assert.Same(routeGroupBuilder, result); // Should return the same builder for chaining
    }

    [Fact]
    public void RequireWristbandJwt_RouteGroupBuilder_ExtensionMethodExists()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(RouteGroupBuilder) }, null);

        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
        Assert.True(method.IsPublic);
        Assert.Equal(typeof(RouteGroupBuilder), method.ReturnType);
    }

    [Fact]
    public void RequireWristbandJwt_RouteGroupBuilder_HasCorrectParameterName()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", new[] { typeof(RouteGroupBuilder) });

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("group", parameters[0].Name);
    }

    [Fact]
    public void RequireWristbandJwt_RouteGroupBuilder_IsExtensionMethod()
    {
        var method = typeof(WristbandJwtValidationExtensions)
            .GetMethod("RequireWristbandJwt", new[] { typeof(RouteGroupBuilder) });

        Assert.NotNull(method);
        Assert.True(method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
    }
}
