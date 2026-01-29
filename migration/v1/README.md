<div align="center">
  <a href="https://wristband.dev">
    <picture>
      <img src="https://assets.wristband.dev/images/email_branding_logo_v1.png" alt="Github" width="297" height="64">
    </picture>
  </a>
  <p align="center">
    Migration instruction from version v0.x to version v1.x
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

# Migration instruction from version v0.x to version v1.x

**Legend:**

- (`-`) indicates the older version of the code that needs to be changed
- (`+`) indicates the new and correct version of the code for version 1.x

<br>

## Table of Contents

- [.NET Version Requirements](#net-version-requirements)
- [Configuration Changes](#configuration-changes)
- [Authorization API Changes](#authorization-api-changes)
- [New Extension Methods for JWT Access](#new-extension-methods-for-jwt-access)

<br>

## .NET Version Requirements

Version 1.x has updated the minimum .NET version requirements.

**Before (v0.x):**
- Supported .NET 6.0 and above

**After (v1.x):**
- Supported .NET 8.0 and above
- .NET 6 and .NET 7 are no longer supported (both reached end-of-life)

**Migration:**
If you're still on .NET 6 or .NET 7, you must upgrade to .NET 8 or higher before migrating to v1.x of this SDK.

<br>

## Configuration Changes

Version 1.x changes how JWT validation is configured to align with ASP.NET Core's standard authentication patterns.

**Before (v0.x):**
```csharp
// Program.cs
using Wristband.AspNet.Auth.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWristbandJwtValidation(options =>
{
-   options.WristbandApplicationDomain = "invotastic.us.wristband.dev";
-   options.JwksCacheMaxSize = 10;  // Optional: defaults to 20
-   options.JwksCacheTtl = TimeSpan.FromHours(12);  // Optional: defaults to null
});
```

**After (v1.x):**
```csharp
// Program.cs
using Wristband.AspNet.Auth.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT authentication to validate tokens using Wristband JWKS
builder.Services.AddAuthentication()
+   .AddJwtBearer(options => options.UseWristbandJwksValidation(
+       wristbandApplicationVanityDomain: "invotastic.us.wristband.dev",
+       jwksCacheMaxSize: 10,  // Optional: defaults to 20
+       jwksCacheTtl: TimeSpan.FromHours(12)  // Optional: defaults to null
+   ));

// Configure authorization and register the WristbandJwt policy
+ builder.Services.AddAuthorization(options => options.AddWristbandJwtPolicy());
```

**Note:** The property name changed from `WristbandApplicationDomain` (v0.x) to `wristbandApplicationVanityDomain` (v1.x parameter) for consistency across Wristband SDKs.

<br>

## Authorization API Changes

Version 1.x introduces a new, simpler authorization API with the `RequireWristbandJwt()` extension method.

### Endpoint Protection Changes

**Before (v0.x):**
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
-       .RequireAuthorization();  // Generic authorization
    }
}
```

**After (v1.x):**
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
+       .RequireWristbandJwt();  // Wristband-specific JWT protection
    }
}
```

### Route Group Protection

**Before (v0.x):**
```csharp
- var protectedGroup = routes.MapGroup("/api/protected")
-     .RequireAuthorization(WristbandJwtAuthorization.PolicyName);
```

**After (v1.x):**
```csharp
+ var protectedGroup = routes.MapGroup("/api/protected")
+     .RequireWristbandJwt();
```

### Policy-Based Authorization (Removed)

The following policy-based APIs from v0.x have been removed:

**Removed in v1.x:**
```csharp
- .RequireAuthorization(WristbandJwtAuthorization.PolicyName);
- .RequireAuthorization(WristbandJwtAuthorization.GetPolicy());
```

**Migration:**

For simple authentication-only endpoints, use the convenience method:

```csharp
+ .RequireWristbandJwt();
```

For complex authorization scenarios (combining with roles, claims, or permissions), use `.RequireAuthorization()` with the "WristbandJwt" policy name:

```csharp
// JWT authentication + admin role
+ routes.MapGet("/api/admin", () => "Admin only")
+     .RequireAuthorization("WristbandJwt", "AdminOnly");

// JWT authentication + custom permission
+ routes.MapGet("/api/users/edit", () => "Edit users")
+     .RequireAuthorization("WristbandJwt", "CanEditUsers");
```

See the [API Reference](../README.md#requirewristbandjwt) section in the main README for more details on complex authorization scenarios.

<br>

## New Extension Methods for JWT Access

Version 1.x introduces new extension methods for accessing JWT data within your endpoints. While these are new features, you may want to migrate existing code that manually extracts JWT data to use these convenient helpers.

### Accessing JWT Token

**Before (v0.x):**
```csharp
routes.MapGet("/api/user/profile", (HttpContext context) =>
{
    // Manual extraction
-   var authHeader = context.Request.Headers["Authorization"].ToString();
-   var token = authHeader.Replace("Bearer ", "");
    
    return Results.Ok(new { token });
})
.RequireAuthorization();
```

**After (v1.x):**
```csharp
routes.MapGet("/api/user/profile", (HttpContext context) =>
{
    // Using new extension method
+   var jwt = context.GetJwt();
    
    return Results.Ok(new { token = jwt });
})
.RequireWristbandJwt();
```

### Accessing JWT Payload

**Before (v0.x):**
```csharp
routes.MapGet("/api/user/profile", (HttpContext context) =>
{
    // Manual claims extraction
-   var userId = context.User.FindFirst("sub")?.Value;
-   var tenantIdClaim = context.User.FindFirst("tnt_id")?.Value;
    
    return Results.Ok(new { userId, tenantId = tenantIdClaim });
})
.RequireAuthorization();
```

**After (v1.x):**
```csharp
routes.MapGet("/api/user/profile", (HttpContext context) =>
{
    // Using new extension method
+   var payload = context.GetJwtPayload();
+   var userId = payload.Sub;
+   var tenantId = payload.Claims?["tnt_id"];
    
    return Results.Ok(new { userId, tenantId });
})
.RequireWristbandJwt();
```

**Key Benefits:**
- Type-safe access to standard JWT claims (Sub, Iss, Exp, etc.)
- Easy access to custom claims via `Claims` dictionary
- No need to manually parse or extract claims from `HttpContext.User`

<br>

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions around migration.

<br/>
