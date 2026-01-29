# Multi-Instance JWT Validation Guide

---

## Overview

This guide shows how to validate JWTs from multiple Wristband applications in a single ASP.NET Core app using the programmatic `WristbandJwtValidator` API.

**Use this approach when:**
- You have multiple Wristband applications (e.g., "app1", "app2") in one ASP.NET Core app
- Each Wristband app has its own JWKS endpoint
- You need fine-grained control over which validator runs for which endpoint

**For single-instance scenarios, use the standard authentication scheme approach instead.**

---

## Why Programmatic Validation?

**The Problem with Authentication Schemes:**

JWT Bearer authentication in ASP.NET Core uses a single JWKS endpoint per scheme. For multi-instance:

```csharp
// ❌ This doesn't work - can only register .AddJwtBearer() once with default name
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation("app1.wristband.dev"))
    .AddJwtBearer(options => options.UseWristbandJwksValidation("app2.wristband.dev"));
```

You could use named schemes, but then you need complex logic to pick which scheme to use.

**The Solution: Programmatic Validation**

Create multiple `WristbandJwtValidator` instances and build a custom authorization handler that picks the right one.

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│  Endpoint: /api/app1/data                       │
│  .RequireApp1Jwt()                              │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│  Authorization Policy: "App1Jwt"                │
│  Requirement: MultiAppJwtRequirement("app1")    │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│  MultiAppJwtHandler                             │
│  - Looks up validator for "app1"                │
│  - Calls validator.Validate(httpContext)        │
│  - Populates context.User with claims           │
└─────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│  Endpoint Handler                               │
│  - context.User is populated                    │
│  - context.GetJwt() works                       │
│  - context.GetJwtPayload() works                │
└─────────────────────────────────────────────────┘
```

---

## Implementation

### Step 1: Create Custom Authorization Requirement

```csharp
using Microsoft.AspNetCore.Authorization;

namespace YourApp.Authorization;

public class MultiAppJwtRequirement : IAuthorizationRequirement
{
    public string AppName { get; }
    
    public MultiAppJwtRequirement(string appName)
    {
        AppName = appName;
    }
}
```

---

### Step 2: Create Custom Authorization Handler

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wristband.AspNet.Auth.Jwt;

namespace YourApp.Authorization;

public class MultiAppJwtHandler : AuthorizationHandler<MultiAppJwtRequirement>
{
    private readonly Dictionary<string, WristbandJwtValidator> _validators;
    
    public MultiAppJwtHandler()
    {
        // Initialize validators for each Wristband application
        _validators = new Dictionary<string, WristbandJwtValidator>
        {
            ["app1"] = WristbandJwtValidator.Create(new WristbandJwtValidatorConfig
            {
                WristbandApplicationVanityDomain = "app1.wristband.dev",
                JwksCacheMaxSize = 20,
                JwksCacheTtl = TimeSpan.FromHours(12)
            }),
            ["app2"] = WristbandJwtValidator.Create(new WristbandJwtValidatorConfig
            {
                WristbandApplicationVanityDomain = "app2.wristband.dev",
                JwksCacheMaxSize = 20,
                JwksCacheTtl = TimeSpan.FromHours(12)
            })
        };
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MultiAppJwtRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }
        
        // Get the validator for the specified app
        if (!_validators.TryGetValue(requirement.AppName, out var validator))
        {
            context.Fail();
            return;
        }
        
        // Validate JWT and populate context.User
        var result = await validator.Validate(httpContext);
        
        if (result.IsValid)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
```

**Key Points:**
- Creates one `WristbandJwtValidator` per Wristband application
- `Validate(httpContext)` validates the JWT AND populates `context.User`
- This enables all Wristband context extensions to work (`GetJwt()`, `GetJwtPayload()`)

---

### Step 3: Create Extension Methods (Optional but Recommended)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace YourApp.Authorization;

public static class CustomJwtExtensions
{
    public static RouteHandlerBuilder RequireApp1Jwt(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization("App1Jwt");
    }
    
    public static RouteHandlerBuilder RequireApp2Jwt(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization("App2Jwt");
    }
    
    // For route groups
    public static RouteGroupBuilder RequireApp1Jwt(this RouteGroupBuilder group)
    {
        return group.RequireAuthorization("App1Jwt");
    }
    
    public static RouteGroupBuilder RequireApp2Jwt(this RouteGroupBuilder group)
    {
        return group.RequireAuthorization("App2Jwt");
    }
    
    // Dynamic version (advanced)
    public static RouteHandlerBuilder RequireCustomJwtValidation(
        this RouteHandlerBuilder builder,
        string appName)
    {
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new MultiAppJwtRequirement(appName))
            .Build();
        
        return builder.RequireAuthorization(policy);
    }
}
```

---

### Step 4: Register in Program.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Wristband.AspNet.Auth.Jwt;
using YourApp.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Register authorization with policies for each app
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("App1Jwt", policy => 
        policy.AddRequirements(new MultiAppJwtRequirement("app1")));
    
    options.AddPolicy("App2Jwt", policy => 
        policy.AddRequirements(new MultiAppJwtRequirement("app2")));
});

// Register the custom authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, MultiAppJwtHandler>();

var app = builder.Build();

// Add authorization middleware
// NOTE: No need for .UseAuthentication() middleware since we're doing 
// JWT validation programmatically in the authorization handler, not via 
// ASP.NET Core's authentication schemes
app.UseAuthorization();

app.Run();
```

**Important Notes:**
- **No `.AddAuthentication()`** - we're not using authentication schemes
- **No `.AddJwtBearer()`** - we're using programmatic validation instead
- **No `.UseAuthentication()`** - authorization middleware is enough
- Each app gets its own policy registered

---

### Step 5: Use in Endpoints

```csharp
using Wristband.AspNet.Auth.Jwt;
using YourApp.Authorization;

// App1 endpoints
app.MapGet("/api/app1/profile", (HttpContext context) =>
{
    // All Wristband extensions work because Validate(httpContext) populated context.User
    var jwt = context.GetJwt();
    var payload = context.GetJwtPayload();
    
    var userId = payload.Sub;
    var tenantId = payload.Claims?["tnt_id"];
    var appId = payload.Claims?["app_id"];
    
    return Results.Ok(new 
    { 
        userId, 
        tenantId, 
        appId,
        app = "app1"
    });
})
.RequireApp1Jwt();

// App2 endpoints
app.MapGet("/api/app2/profile", (HttpContext context) =>
{
    var payload = context.GetJwtPayload();
    
    return Results.Ok(new 
    { 
        userId = payload.Sub,
        tenantId = payload.Claims?["tnt_id"],
        app = "app2"
    });
})
.RequireApp2Jwt();

// Route groups
var app1Group = app.MapGroup("/api/app1")
    .RequireApp1Jwt();

app1Group.MapGet("/data", () => Results.Ok("App1 data"));
app1Group.MapGet("/settings", () => Results.Ok("App1 settings"));

var app2Group = app.MapGroup("/api/app2")
    .RequireApp2Jwt();

app2Group.MapGet("/data", () => Results.Ok("App2 data"));
app2Group.MapGet("/settings", () => Results.Ok("App2 settings"));
```

---

## Advanced: Dynamic App Selection

For scenarios where the app is determined from the request (e.g., subdomain, header, route parameter):

```csharp
public class DynamicMultiAppJwtHandler : AuthorizationHandler<DynamicMultiAppJwtRequirement>
{
    private readonly Dictionary<string, WristbandJwtValidator> _validators;
    
    public DynamicMultiAppJwtHandler()
    {
        // Initialize validators for each Wristband application (same as MultiAppJwtHandler)
        _validators = new Dictionary<string, WristbandJwtValidator>
        {
            ["app1"] = WristbandJwtValidator.Create(new WristbandJwtValidatorConfig
            {
                WristbandApplicationVanityDomain = "app1.wristband.dev",
                JwksCacheMaxSize = 20,
                JwksCacheTtl = TimeSpan.FromHours(12)
            }),
            ["app2"] = WristbandJwtValidator.Create(new WristbandJwtValidatorConfig
            {
                WristbandApplicationVanityDomain = "app2.wristband.dev",
                JwksCacheMaxSize = 20,
                JwksCacheTtl = TimeSpan.FromHours(12)
            })
        };
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DynamicMultiAppJwtRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }
        
        // Determine app from request context
        var appName = DetermineAppFromRequest(httpContext);
        
        if (appName == null || !_validators.TryGetValue(appName, out var validator))
        {
            context.Fail();
            return;
        }
        
        var result = await validator.Validate(httpContext);
        
        if (result.IsValid)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
    
    private string? DetermineAppFromRequest(HttpContext context)
    {
        // Option 1: From subdomain
        var host = context.Request.Host.Host;
        if (host.StartsWith("app1.")) return "app1";
        if (host.StartsWith("app2.")) return "app2";
        
        // Option 2: From header
        var appHeader = context.Request.Headers["X-App-Id"].ToString();
        if (!string.IsNullOrEmpty(appHeader)) return appHeader;
        
        // Option 3: From route parameter
        var routeData = context.GetRouteData();
        if (routeData.Values.TryGetValue("appId", out var appId))
        {
            return appId?.ToString();
        }
        
        return null;
    }
}
```

---

## Benefits of This Approach

✅ **Full control** - You decide which validator runs for which endpoint  
✅ **Clean endpoint syntax** - Custom extension methods like `.RequireApp1Jwt()`  
✅ **Works with Wristband extensions** - `GetJwt()` and `GetJwtPayload()` work seamlessly  
✅ **No authentication scheme conflicts** - Each validator is independent  
✅ **Flexible** - Can determine app dynamically from request context  
✅ **Type-safe** - Compile-time guarantees for policy names and requirements  
✅ **Independent JWKS caching** - Each app maintains its own key cache with separate TTLs

---

## Comparison: Single-Instance vs Multi-Instance

### Single-Instance (Recommended for Most Users)

**Setup:**
```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation("app1.wristband.dev"));

builder.Services.AddAuthorization(options => options.AddWristbandJwtPolicy());
```

**Usage:**
```csharp
app.MapGet("/api/data", () => "Hello")
    .RequireWristbandJwt();
```

---

### Multi-Instance (This Guide)

**Setup:**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("App1Jwt", policy => 
        policy.AddRequirements(new MultiAppJwtRequirement("app1")));
    options.AddPolicy("App2Jwt", policy => 
        policy.AddRequirements(new MultiAppJwtRequirement("app2")));
});

builder.Services.AddSingleton<IAuthorizationHandler, MultiAppJwtHandler>();
```

**Usage:**
```csharp
app.MapGet("/api/app1/data", () => "Hello")
    .RequireApp1Jwt();

app.MapGet("/api/app2/data", () => "Hello")
    .RequireApp2Jwt();
```

---

## Testing

```csharp
[Fact]
public async Task MultiAppJwtHandler_ValidatesApp1Jwt_Succeeds()
{
    var handler = new MultiAppJwtHandler();
    var requirement = new MultiAppJwtRequirement("app1");
    var user = new ClaimsPrincipal();
    var authContext = new AuthorizationHandlerContext(
        new[] { requirement },
        user,
        httpContext);
    
    // Mock HttpContext with valid App1 JWT in Authorization header
    var httpContext = new DefaultHttpContext();
    var validApp1Token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."; // Valid JWT for app1
    httpContext.Request.Headers["Authorization"] = $"Bearer {validApp1Token}";
    
    await handler.HandleRequirementAsync(authContext, requirement);

    Assert.True(authContext.HasSucceeded);
    Assert.NotNull(httpContext.User);
    Assert.Equal("app1-user-id", httpContext.User.FindFirst("sub")?.Value);
}
```

---

## Troubleshooting

### Issue: `context.User` is null in endpoint

**Solution:** Make sure you're calling the `Validate(HttpContext)` overload in your handler, not the `Validate(string)` overload.

```csharp
// ✅ Correct - populates context.User
var result = await validator.Validate(httpContext);

// ❌ Wrong - doesn't populate context.User
var authHeader = httpContext.Request.Headers["Authorization"].ToString();
var token = validator.ExtractBearerToken(authHeader);
var result = await validator.Validate(token);
```

### Issue: `GetJwt()` or `GetJwtPayload()` throws exception

**Solution:** Ensure validation succeeded before accessing extensions:

```csharp
var result = await validator.Validate(httpContext);
if (result.IsValid)
{
    context.Succeed(requirement);
    // Extensions will work in endpoint
}
```

### Issue: Handler never runs

**Solution:** Make sure you registered both the policy AND the handler:

```csharp
// Register policy
builder.Services.AddAuthorization(options => {
    options.AddPolicy("App1Jwt", policy => 
        policy.AddRequirements(new MultiAppJwtRequirement("app1")));
});

// Register handler
builder.Services.AddSingleton<IAuthorizationHandler, MultiAppJwtHandler>();
```

---

## Questions

For questions about multi-instance JWT validation, reach out to the Wristband team at <support@wristband.dev>.

For single-instance scenarios, see the main [aspnet-jwt README](./README.md).
