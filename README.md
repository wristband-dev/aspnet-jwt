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

This ASP.NET Core SDK validates JWT access tokens issued by Wristband for user or machine authentication. It uses the Wristband JWKS endpoint to resolve signing keys and verify RS256 signatures. Validation includes issuer verification, lifetime checks, and signature validation using cached keys.

The SDK provides two integration options:
- **Middleware Integration:** Integrates with ASP.NET Core's authentication pipeline for declarative endpoint protection at the route or route group level using the `RequireWristbandJwt()` extension method.
- **Programmatic Validation:** Direct API for custom authorization handlers, service layers, and advanced scenarios.

You can learn more about JWTs in Wristband in our documentation:

- [JWTs and Signing Keys](https://docs.wristband.dev/docs/json-web-tokens-jwts-and-signing-keys)

<br>

---

<br>

## Table of Contents

- [Migrating From Older SDK Versions](#migrating-from-older-sdk-versions)
- [Requirements](#requirements)
- [Installation](#installation)
- [Wristband Configuration](#wristband-configuration)
- [Usage](#usage)
  - [Option 1: Middleware Integration](#option-1-middleware-integration-recommended-for-apis)
    - [1) Register the SDK](#1-register-the-sdk)
    - [2) Protect Your APIs](#2-protect-your-apis)
    - [3) Access JWT Data in Your Endpoints](#3-access-jwt-data-in-your-endpoints)
  - [Option 2: Programmatic Validation](#option-2-programmatic-validation-for-custom-scenarios)
    - [1) Create a Validator Instance](#1-create-a-validator-instance)
    - [2) Extract and Validate Tokens](#2-extract-and-validate-tokens)
- [JWKS Caching and Expiration](#jwks-caching-and-expiration)
- [SDK Configuration Options](#sdk-configuration-options)
- [API Reference](#api-reference)
  - [Middleware API](#middleware-api)
    - [UseWristbandJwksValidation](#usewristbandjwksvalidationstring-int-timespan)
    - [AddWristbandJwtPolicy](#addwristbandjwtpolicy)
    - [RequireWristbandJwt](#requirewristbandjwt)
    - [GetJwt](#getjwt)
    - [GetJwtPayload](#getjwtpayload)
  - [Programmatic Validator API](#programmatic-validator-api)
    - [WristbandJwtValidator.Create](#wristbandjwtvalidatorcreatewristbandjwtvalidatorconfig-config)
    - [ExtractBearerToken](#extractbearertokenstring-authorizationheader)
    - [Validate (string)](#validatestring-token)
    - [Validate (HttpContext)](#validatehttpcontext-context)
    - [JwtValidationResult](#jwtvalidationresult)
    - [JWTPayload](#jwtpayload)
- [Questions](#questions)

<br>

## Migrating From Older SDK Versions

On an older version of our SDK? Check out our migration guide:

- [Instructions for migrating to Version 1.x (latest)](migration/v1/README.md)

<br>

## Requirements

Before installing, ensure you have:

- [.NET SDK](https://dotnet.microsoft.com/download) >= 8.0
- Your preferred package manager (dotnet CLI, NuGet Package Manager)

<br>

## Installation

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
  <PackageReference Include="Wristband.AspNet.Auth.Jwt" Version="1.0.0" />
</ItemGroup>
```

<br>

## Wristband Configuration

First, you'll need to make sure you have an Application in your Wristband Dashboard account. If you haven't done so yet, refer to our docs on [Creating an Application](https://docs.wristband.dev/docs/setting-up-your-wristband-account).

**Make sure to copy the Application Vanity Domain for next steps, which can be found in "Application Settings" for your Wristband Application.**

<br/>

## Usage

### Option 1: Middleware Integration (Recommended for APIs)

This approach integrates with ASP.NET Core's authentication pipeline, allowing you to declaratively protect endpoints using authorization policies.

#### 1) Register the SDK

In your `Program.cs` file, register the SDK and ensure authentication and authorization middlewares are in place:

```csharp
using Wristband.AspNet.Auth.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Register JWT Bearer authentication with Wristband JWKS validation
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation(
        wristbandApplicationVanityDomain: "<your-wristband-application-vanity-domain>",
        jwksCacheMaxSize: 10,  // Optional: defaults to 20
        jwksCacheTtl: TimeSpan.FromHours(12)  // Optional: defaults to no expiration
    ));

// Configure authorization and register the WristbandJwt policy
builder.Services.AddAuthorization(options => options.AddWristbandJwtPolicy());

var app = builder.Build();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

<br>

#### 2) Protect Your APIs

Once configured, you can protect individual endpoints or entire route groups:

**Protecting individual endpoints**
```csharp
using Wristband.AspNet.Auth.Jwt;

public static class ApiRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/protected/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        })
        .RequireWristbandJwt();
    }
}
```

**Protecting route groups**
```csharp
using Wristband.AspNet.Auth.Jwt;

public static class ApiRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var protectedGroup = routes.MapGroup("/api/protected")
            .RequireWristbandJwt();  // All endpoints in this group require JWT auth

        protectedGroup.MapGet("/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        });

        protectedGroup.MapPost("/update", (HttpContext context) =>
        {
            return Results.Ok("Updated!");
        });
    }
}
```

<br/>

#### 3) Access JWT Data in Your Endpoints

Once authenticated, you can access the JWT token and payload using the provided extension methods:

```csharp
using Wristband.AspNet.Auth.Jwt;

routes.MapGet("/api/user/profile", (HttpContext context) =>
{
    // Get the raw JWT token from the Authorization header
    var jwt = context.GetJwt();

    // Get the validated JWT payload from authenticated claims
    var payload = context.GetJwtPayload();

    // Access standard JWT claims
    var userId = payload.Sub;
    var issuer = payload.Iss;
    var expiration = payload.Exp;
    
    // Access custom Wristband claims
    var tenantId = payload.Claims?["tnt_id"];
    var appId = payload.Claims?["app_id"];

    return Results.Ok(new 
    { 
        userId, 
        tenantId,
        appId,
        expiration
    });
})
.RequireWristbandJwt();
```

<br/>

### Option 2: Programmatic Validation (For Custom Scenarios)

This approach provides direct access to the JWT validation API, useful for custom authorization handlers, service layers, background services, or multi-application scenarios.

#### 1) Create a Validator Instance

Create a validator instance in your application startup or service configuration:

```csharp
using Wristband.AspNet.Auth.Jwt;

var validatorConfig = new WristbandJwtValidatorConfig
{
    WristbandApplicationVanityDomain = "invotastic.us.wristband.dev",
    JwksCacheMaxSize = 20,
    JwksCacheTtl = TimeSpan.FromHours(1)
};

var validator = WristbandJwtValidator.Create(validatorConfig);
```

#### 2) Extract and Validate Tokens

Use the validator to extract Bearer tokens from Authorization headers and validate them:

**In a custom authorization handler:**
```csharp
public class CustomAuthHandler : AuthorizationHandler<CustomRequirement>
{
    private readonly WristbandJwtValidator _validator;

    public CustomAuthHandler(WristbandJwtValidator validator)
    {
        _validator = validator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext)
        {
            var result = await _validator.Validate(httpContext);
            
            if (result.IsValid)
            {
                // httpContext.User is now populated with JWT claims
                context.Succeed(requirement);
            }
        }
    }
}
```

**In custom middleware:**
```csharp
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WristbandJwtValidator _validator;

    public JwtValidationMiddleware(RequestDelegate next, WristbandJwtValidator validator)
    {
        _next = next;
        _validator = validator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var result = await _validator.Validate(context);
        
        if (result.IsValid)
        {
            // context.User is now populated with JWT claims
            await _next(context);
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = result.ErrorMessage });
        }
    }
}
```

**In a service layer:**
```csharp
public class UserService
{
    private readonly WristbandJwtValidator _validator;

    public UserService(WristbandJwtValidator validator)
    {
        _validator = validator;
    }

    public async Task<User?> GetUserFromToken(string authHeader)
    {
        try
        {
            var token = _validator.ExtractBearerToken(authHeader);
            var result = await _validator.Validate(token);

            if (result.IsValid)
            {
                return await FetchUserById(result.Payload?.Sub);
            }

            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
```

<br>

## JWKS Caching and Expiration

The SDK automatically retrieves and caches JSON Web Key Sets (JWKS) from your Wristband application's domain to validate incoming access tokens. By default, keys are cached in memory and reused across requests to avoid unnecessary network calls.

You can control how the SDK handles this caching behavior using two optional configuration values: `JwksCacheMaxSize` and `JwksCacheTtl`.

**For Middleware:**
```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation(
        wristbandApplicationVanityDomain: "invotastic.us.wristband.dev",
        jwksCacheMaxSize: 10, // Keep at most 10 keys in cache
        jwksCacheTtl: TimeSpan.FromHours(12) // Expire keys after 12 hours
    ));
```

**For Programmatic Validator:**
```csharp
var validatorConfig = new WristbandJwtValidatorConfig
{
    WristbandApplicationVanityDomain = "invotastic.us.wristband.dev",
    JwksCacheMaxSize = 10,  // Keep at most 10 keys in cache
    JwksCacheTtl = TimeSpan.FromHours(12)  // Expire keys after 12 hours
};
```

If `JwksCacheTtl` is not set, cached keys remain available until evicted by the cache size limit.

<br>

## SDK Configuration Options

The `WristbandJwtValidatorConfig` is used for both Middleware Configuration and Programmatic Validator Configuration:

| JWT Validator Config Option | Type | Required | Description |
| --------------------------- | ---- | -------- | ----------- |
| JwksCacheMaxSize | int | No | Maximum number of JWKs to cache in memory. When exceeded, the least recently used keys are evicted. Defaults to 20. |
| JwksCacheTtl | `TimeSpan` | No | Time-to-live for cached JWKs. If not set, keys remain in cache until eviction by size limit. |
| WristbandApplicationVanityDomain | string | Yes |	The Wristband application vanity domain used to construct the JWKS endpoint URL for verifying tokens. Example: `myapp.wristband.dev`. |

<br>

## API Reference

### Middleware API

#### `UseWristbandJwksValidation(string, int?, TimeSpan?)`

Extension method for `JwtBearerOptions` that configures Wristband JWKS validation.

**Parameters:**
- `wristbandApplicationVanityDomain` - The Wristband application vanity domain (required)
- `jwksCacheMaxSize` - Maximum number of JWKs to cache (optional, defaults to 20)
- `jwksCacheTtl` - Time-to-live for cached JWKs (optional, defaults to null)

**Returns:** The `JwtBearerOptions` for method chaining

**Example:**
```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation(
        "invotastic.us.wristband.dev",
        jwksCacheMaxSize: 10,
        jwksCacheTtl: TimeSpan.FromHours(12)
    ));
```

#### `AddWristbandJwtPolicy()`

Extension method for `AuthorizationOptions` that adds the "WristbandJwt" authorization policy.

**Returns:** The `AuthorizationOptions` for method chaining

**Example:**
```csharp
builder.Services.AddAuthorization(options => options.AddWristbandJwtPolicy());
```

**⚠️ IMPORTANT:** When using the `Wristband.AspNet.Auth` package, this method is re-exported for convenience. Use the `Wristband.AspNet.Auth` namespace to access all Wristband authorization policies in one place.

#### `RequireWristbandJwt()`

Extension methods for `RouteHandlerBuilder` and `RouteGroupBuilder` that require Wristband JWT bearer token authentication based on the "WristbandJwt" authorization policy name.

**Returns:**
- The builder for chaining

**Examples:**

**For individual routes:**
```csharp
routes.MapGet("/api/protected", () => "Protected")
    .RequireWristbandJwt();
```

**For route groups:**
```csharp
var group = routes.MapGroup("/api/protected")
    .RequireWristbandJwt();

group.MapGet("/data", () => "Data");
group.MapPost("/update", () => "Updated");
```

**For complex authorization (combining with roles, claims, or permissions):**

The `RequireWristbandJwt()` convenience method is for simple authentication-only scenarios. For endpoints requiring additional authorization beyond authentication, use `.RequireAuthorization()` with multiple policy names:

```csharp
// JWT authentication && admin role -> policies must be satisfied in order listed
routes.MapGet("/api/admin", () => "Admin only")
    .RequireAuthorization("WristbandJwt", "AdminOnly");

// JWT authentication && custom permission claim -> policies must be satisfied in order listed
routes.MapGet("/api/users/edit", () => "Edit users")
    .RequireAuthorization("WristbandJwt", "CanEditUsers");
```

Alternatively, you can create combined policies in your authorization configuration for commonly used combinations. See the [ASP.NET Core Authorization documentation](https://learn.microsoft.com/aspnet/core/security/authorization/policies) for more information on creating custom policies.

<br>

#### `GetJwt()`

Extension method for `HttpContext` that gets the raw JWT token from the Authorization header.

**Returns:**
- The JWT token string, or null if not present

**Example:**
```csharp
var jwt = context.GetJwt();
```

#### `GetJwtPayload()`

Extension method for `HttpContext` that gets the validated JWT payload from the authenticated user's claims. This method assumes JWT authentication has already been performed and the user is authenticated.

**Returns:**
- The JWT payload object containing all claims

**Example:**
```csharp
var payload = context.GetJwtPayload();
var userId = payload.Sub;
var tenantId = payload.Claims?["tnt_id"];
```

<br/>

### Programmatic Validator API

#### `WristbandJwtValidator.Create(WristbandJwtValidatorConfig config)`

Creates a new WristbandJwtValidator instance.

**Parameters:**
- `config` - Configuration for the JWT validator

**Returns:**
- Configured `WristbandJwtValidator` instance

**Throws:**
- `ArgumentNullException` - When config is null
- `ArgumentException` - When WristbandApplicationVanityDomain is not provided

**Example:**
```csharp
var config = new WristbandJwtValidatorConfig
{
    WristbandApplicationVanityDomain = "invotastic.us.wristband.dev"
};
var validator = WristbandJwtValidator.Create(config);
```

#### `ExtractBearerToken(string authorizationHeader)`

A convenience method that extracts the JWT token from a "Bearer" Authorization header string. Useful when you have an Authorization header but not an HttpContext, or when you need the raw token for logging or forwarding to other services.

**Parameters:**
- `authorizationHeader` - The Authorization header value (e.g., "Bearer eyJ...")

**Returns:**
- The extracted JWT token string

**Throws:**
- `ArgumentException` - When the header is missing, malformed, or does not use the Bearer scheme

**Example:**
```csharp
var authHeader = context.Request.Headers["Authorization"].ToString();
var token = validator.ExtractBearerToken(authHeader);
```

#### `Validate(string token)`

Validates a JWT token string and returns the result with the decoded payload.

**When to use:** Use this method when you have a JWT token as a string (not from an HttpContext). Common scenarios include service layers, background jobs, unit tests, or when you only need the payload without populating HttpContext.User.

**Parameters:**
- `token` - The JWT token to validate

**Returns:**
- `Task<JwtValidationResult>` - Validation result indicating success/failure and containing the decoded payload or error message

**Example:**
```csharp
var result = await validator.Validate(token);
if (result.IsValid)
{
    var userId = result.Payload?.Sub;
    // Use the validated token...
}
else
{
    // Handle validation error: result.ErrorMessage
}
```

#### `Validate(HttpContext context)`

Validates a JWT token from the Authorization header and automatically populates the `HttpContext.User` with claims from the validated token.

This is the preferred method for authentication scenarios as it provides seamless integration with ASP.NET Core's authentication system, allowing downstream code to use `HttpContext.User` and related extension methods like `GetJwt()` and `GetJwtPayload()`.

**When to use:** Use this method in custom authorization handlers, middleware, or any scenario where you have access to HttpContext and want the SDK to automatically:

- Extract the token from the Authorization header
- Validate the token
- Populate `HttpContext.User` with claims from the validated token

**Parameters:**
- `context` - The HTTP context containing the Authorization header

**Returns:**
- `Task<JwtValidationResult>` - Validation result indicating success/failure. If successful, context.User is populated with JWT claims.

**Throws:**
- `ArgumentNullException` - When context is null

**Example:**
```csharp
var result = await validator.Validate(httpContext);
if (result.IsValid)
{
    // Access claims via the validation result
    var userId = result.Payload?.Sub;
    
    // Or via HttpContext extensions
    var payload = httpContext.GetJwtPayload();
    var tenantId = payload.Claims?["tnt_id"];
    
    // Or via HttpContext.User directly
    var userIdFromClaims = httpContext.User.FindFirst("sub")?.Value;
}
else
{
    // Handle validation failure: result.ErrorMessage
}
```

**Note:** For advanced multi-instance scenarios, see our [Multi-Instance JWT Validation Guide](docs/MULTI_INSTANCE_JWT_GUIDE.md).

#### `JwtValidationResult`

Result object returned by JWT validation.

**Properties:**
- `IsValid` (bool) - Indicates whether the token is valid
- `Payload` (JWTPayload?) - Decoded JWT payload, present when IsValid is true
- `ErrorMessage` (string?) - Error message explaining validation failure, present when IsValid is false

#### `JWTPayload`

Decoded JWT payload containing standard and custom claims.

**Properties:**
- `Iss` (string?) - Issuer (iss claim)
- `Sub` (string?) - Subject, typically the user ID (sub claim)
- `Aud` (string[]?) - Audience as an array (aud claim)
- `Exp` (long?) - Expiration time as Unix timestamp in seconds (exp claim)
- `Iat` (long?) - Issued at time as Unix timestamp in seconds (iat claim)
- `Nbf` (long?) - Not before time as Unix timestamp in seconds (nbf claim)
- `Jti` (string?) - JWT ID (jti claim)
- `Claims` (Dictionary<string, string>?) - All claims as a dictionary (including standard and custom claims)

**Methods:**
- `GetAudienceAsString()` - Returns the first audience as a string, or null if no audiences are present

<br>

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this SDK.

<br/>
