<div align="center">
  <a href="https://wristband.dev">
    <picture>
      <img src="https://assets.wristband.dev/images/email_branding_logo_v1.png" alt="Github" width="297" height="64">
    </picture>
  </a>
  <p align="center">
    Enterprise-ready auth that is secure by default, truly multi-tenant, and ungated for small businesses.
  </p>
  <p align="center">
    <b>
      <a href="https://wristband.dev">Website</a> • 
      <a href="https://docs.wristband.dev/">Documentation</a>
    </b>
  </p>
</div>

<br/>

---

<br/>

# Wristband JWT Validation SDK for ASP.NET Core

[![NuGet](https://img.shields.io/nuget/v/Wristband.AspNet.Auth.Jwt?label=NuGet)](https://www.nuget.org/packages/Wristband.AspNet.Auth.Jwt/)
[![version number](https://img.shields.io/github/v/release/wristband-dev/aspnet-jwt?color=green&label=version)](https://github.com/wristband-dev/aspnet-jwt/releases)
[![Actions Status](https://github.com/wristband-dev/aspnet-jwt/workflows/Test/badge.svg)](https://github.com/wristband-dev/aspnet-jwt/actions)
[![License](https://img.shields.io/github/license/wristband-dev/aspnet-jwt)](https://github.com/wristband-dev/aspnet-jwt/blob/main/LICENSE)

This ASP.NET Core SDK validates JWT access tokens issued by Wristband for user or machine authentication. It uses the Wristband JWKS endpoint to resolve signing keys and verify RS256 signatures. Validation includes issuer verification, lifetime checks, and signature validation using cached keys. The SDK integrates with ASP.NET Core's built-in authentication and authorization system. Once configured, it enables authorization policies to be enforced at the endpoint level using `RequireAuthorization`. This allows developers to declaratively protect routes and ensure that only valid, Wristband-issued access tokens can access secured APIs.

You can learn more about JWTs in Wristband in our documentation:

- [JWTs and Signing Keys](https://docs.wristband.dev/docs/json-web-tokens-jwts-and-signing-keys)

## Requirements

This SDK is supported for versions .NET 6 and above.

## 1) Installation

This SDK is available in [Nuget](https://www.nuget.org/organization/wristband) and can be installed with the `dotnet` CLI:
```sh
dotnet add package Wristband.AspNet.Auth.Jwt
```

Or it can also be installed through the Package Manager Console as well:
```sh
Install-Package Wristband.AspNet.Auth.Jwt
```

You should see the dependency added to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Wristband.AspNet.Auth.Jwt" Version="0.1.0" />
</ItemGroup>
```

## 2) Wristband Configuration

First, you'll need to make sure you have an Application in your Wristband Dashboard account. If you haven't done so yet, refer to our docs on [Creating an Application](https://docs.wristband.dev/docs/setting-up-your-wristband-account).

**Make sure to copy the Application Vanity Domain for next steps, which can be found in "Application Settings" for your Wristband Application.**

## 3) SDK Configuration

To enable proper communication between your ASP.NET server and Wristband, add the following configuration section to your `appsettings.json` file, replacing all placeholder values with your own.

```json
"WristbandJwtConfig": {
  "WristbandApplicationDomain": "sometest-account.us.wristband.dev"
},
```

## 4) Register the SDK

In your `Program.cs` file, you'll need to register the SDK as well as ensure authentication and authorization middlewares are in place:

```csharp
// Program.cs
using Wristband.AspNet.Auth.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Register Wristband JWT validation with JWKS
builder.Services.AddWristbandJwtValidation(options =>
{
    var config = builder.Configuration.GetSection("WristbandJwtConfig");
    options.WristbandApplicationDomain = config["APPLICATION_DOMAIN"];
});

// Configure authorization to allow usage of RequiresAuthorization() on endpoints.
builder.Services.AddAuthorization();

...

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

...
```

## 5) Protect Your APIs with Authorization

Once Wristband JWT validation is wired into your ASP.NET Core app, protecting your API endpoints becomes seamless. You can enforce authentication by adding authorization policies directly to your route definitions. The SDK integrates with ASP.NET Core’s `RequireAuthorization()` mechanism, so you can secure endpoints using the default policy or Wristband-specific ones—depending on your needs. Whether you're building internal tools, public APIs, or hybrid access layers, the options below give you flexibility to match your access control strategy.

There’s a few ways to use this with your APIs:

**1. Using the no-arg default - Uses the default policy (any authentication)**
```csharp
// ApiRoutes.cs
using Wristband.AspNet.Auth.Jwt;

public static class ApiRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/protected/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        })
        .WithName("GetProtectedData")
        .RequireAuthorization();
    }
}
```

**2. Using the string-based policy name - Explicitly requires Wristband JWT authentication**
```csharp
// ApiRoutes.cs
using Wristband.AspNet.Auth.Jwt;

public static class ApiRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/protected/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        })
        .WithName("GetProtectedData")
        .RequireAuthorization(WristbandJwtAuthorization.PolicyName);
    }
}
```

**3. Using the direct policy object - Useful for combining with other policies**
```csharp
// ApiRoutes.cs
using Wristband.AspNet.Auth.Jwt;

public static class ApiRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/protected/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        })
        .WithName("GetProtectedData")
        .RequireAuthorization(WristbandJwtAuthorization.GetPolicy());
    }
}
```

## JWKS Caching and Expiration

The SDK automatically retrieves and caches JSON Web Key Sets (JWKS) from your Wristband application's domain to validate incoming access tokens. By default, keys are cached in memory and reused across requests to avoid unnecessary network calls.

You can control how the SDK handles this caching behavior using two optional configuration values: `JwksCacheMaxSize` and `JwksCacheTtl`.

**Set a limit on how many keys to keep in memory:**
```csharp
// Program.cs
builder.Services.AddWristbandJwtValidation(options =>
{
    ...

    // Keep at most 10 keys in cache
    options.JwksCacheMaxSize = 10;
});
```

**Set a time-to-live duration for each key:**
```csharp
// Program.cs
builder.Services.AddWristbandJwtValidation(options =>
{
    ...

    // Expire keys from cache after 12 hours
    options.JwksCacheTtl = TimeSpan.FromHours(12);
});
```

If `JwksCacheTtl` is not set, cached keys remain available until evicted by the cache size limit.

<br>

## SDK Configuration Options

| JWT Validation Option | Type | Required | Description |
| --------------------- | ---- | -------- | ----------- |
| JwksCacheMaxSize | int | No | Maximum number of JWKs to cache in memory. When exceeded, the least recently used keys are evicted. Defaults to 20. |
| JwksCacheTtl | `TimeSpan` | No | Time-to-live for cached JWKs. If not set, keys remain in cache until eviction by size limit. |
| WristbandApplicationDomain | string | Yes | Yes	The Wristband vanity domain used to construct the JWKS endpoint URL for verifying tokens. Example: `myapp.wristband.dev`. |

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this SDK.

<br/>
