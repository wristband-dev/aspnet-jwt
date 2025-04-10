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

[![NuGet](https://img.shields.io/nuget/v/Wristband.AspNet.Auth.M2M?label=NuGet)](https://www.nuget.org/packages/Wristband.AspNet.Auth.M2M/)
[![version number](https://img.shields.io/github/v/release/wristband-dev/aspnet-m2m-auth?color=green&label=version)](https://github.com/wristband-dev/aspnet-m2m-auth/releases)
[![Actions Status](https://github.com/wristband-dev/aspnet-m2m-auth/workflows/Test/badge.svg)](https://github.com/wristband-dev/aspnet-m2m-auth/actions)
[![License](https://img.shields.io/github/license/wristband-dev/aspnet-m2m-auth)](https://github.com/wristband-dev/aspnet-m2m-auth/blob/main/LICENSE)

This ASP.NET Core SDK enables Wristband machine-to-machine (M2M) OAuth2 clients to securely retrieve, cache, and refresh access tokens. Designed for server-to-server communication, it automates M2M token management with zero user interaction.

You can learn more about how authentication works in Wristband in our documentation:

- [Machine-to-machine Integration Pattern](https://docs.wristband.dev/docs/machine-to-machine-integration)

## Requirements

This SDK is supported for versions .NET 6 and above.

## 1) Installation

This SDK is available in [Nuget](https://www.nuget.org/organization/wristband) and can be installed with the `dotnet` CLI:
```sh
dotnet add package Wristband.AspNet.Auth.M2M
```

Or it can also be installed through the Package Manager Console as well:
```sh
Install-Package Wristband.AspNet.Auth.M2M
```

You should see the dependency added to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Wristband.AspNet.Auth.M2M" Version="0.1.0" />
</ItemGroup>
```

## 2) Wristband Configuration

First, you'll need to make sure you have an Application in your Wristband Dashboard account. If you haven't done so yet, refer to our docs on [Creating an Application](https://docs.wristband.dev/docs/setting-up-your-wristband-account).
- For new Wristband Applications, you can give any dummy value for the Login Url, such as `https://example.com`, since M2M auth doesn't rely on Login URLs.
- **Make sure to copy the Application Vanity Domain for next steps, which can be found in "Application Settings" for your Wristband Application.**

Then, you'll create a Machine-to-machine OAuth2 Client under that Application while still in the Dashboard.
- **Make sure to have your OAuth2 Client's Client Id and Client Secret handy for next steps, which you'll have the opportunity to copy during creation.**

The Application Vanity Domain, Client ID, and Client Secret values are needed to configure your ASP.NET server.

## 3) SDK Configuration

There are both secret and non-secret values we'll need to set up for the SDK.

### Non-Secret Values Configuration

To enable proper communication between your ASP.NET server and Wristband, add the following configuration section to your `appsettings.json` file, replacing all placeholder values with your own.

```json
"WristbandM2MAuthConfig": {
  "ClientId": "--some-identifier--",
  "WristbandApplicationDomain": "sometest-account.us.wristband.dev"
},
```

### Secret Values Configuration

To configure the Client Secret that the SDK relies on in a secure manner during local testing, you can use .NET User Secrets:

1. Initialize user secrets in your project:
```sh
dotnet user-secrets init
```

This will add a "UserSecretsId" to your `.csproj` file that looks like this:
```xml
<PropertyGroup>
  <UserSecretsId>a-randomly-generated-guid</UserSecretsId>
</PropertyGroup>
```

2. Set your secrets using the CLI:
```sh
dotnet user-secrets set "WristbandM2MAuthConfig:ClientSecret" "your-client-secret"
```

Alternatively, you can manage secrets through Visual Studio by right-clicking your project and selecting "Manage User Secrets". Then add the following to `secrets.json`:

```json
{
  "WristbandM2MAuthConfig": {
    "ClientSecret": "your-client-secret",
  }
}
```

3. During development, the secrets will automatically be loaded when you create your WebApplication builder for the following methods:
- A `secrets.json` in development, or,
- Environment variables prefixed with `ASPNETCORE_`

```csharp
var builder = WebApplication.CreateBuilder(args);
```

You can also explicitly load secrets through the User Secrets configuration provider:

```csharp
builder.Configuration.AddUserSecrets<Program>();
```

Or you can explicitly load from a JSON file:
```csharp
builder.Configuration.AddJsonFile("mysecrets.json", optional: true);
```

In production, another alternative to environment variables is a secure configuration management system:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

> [!NOTE]
> User secrets are for development only. For production, use environment variables or your platform's secure configuration management system.

## 4) Register SDK and Get Token on Server Startup

In your `Program.cs` file, you'll need to do two things: register the SDK and fetch an access token during server startup.

### SDK Registration

There are two configuration approaches to registering the M2M auth SDK:

#### Default Singleton Service

A default singleton service in C# is ideal when you only need one shared instance of a service throughout your server, with no variations in behavior or configuration.

```csharp
// Program.cs
using Wristband.AspNet.Auth.M2M;

var builder = WebApplication.CreateBuilder(args);

// Register Wristband M2M authentication service.
builder.Services.AddWristbandM2MAuth(options =>
{
    var m2mAuthConfig = builder.Configuration.GetSection("WristbandM2MAuthConfig");
    options.WristbandApplicationDomain = m2mAuthConfig["WristbandApplicationDomain"];
    options.ClientId = m2mAuthConfig["ClientId"];
    options.ClientSecret = m2mAuthConfig["ClientSecret"];
});

...
```

#### Named Services

Named services (or multiple registered instances using different OAuth2 Clients) allow you to configure and use multiple M2M clients in the same server.

You can accomodate this pattern in your `appsettings.json` by structuring similar to the following:

```json
{
  "WristbandM2MAuthConfig": {
    "auth01": {
      "ClientId": "--some-identifier--",
      "WristbandApplicationDomain": "sometest-account.us.wristband.dev"
    },
    "auth02": {
      "ClientId": "--another-identifier--",
      "WristbandApplicationDomain": "sometest-account.us.wristband.dev"
    }
  },
}
```

Then in `Program.cs`, you can register the different M2M OAuth2 clients as follows:

```csharp
// Program.cs
using Wristband.AspNet.Auth.M2M;

builder.Services.AddWristbandM2MAuth("auth01", options =>
{
    var m2mAuth01Config = builder.Configuration.GetSection("WristbandM2MAuthConfig:auth01");
    options.WristbandApplicationDomain = m2mAuth01Config["WristbandApplicationDomain"];
    options.ClientId = m2mAuth01Config["ClientId"];
    options.ClientSecret = m2mAuth01Config["ClientSecret"];
});
builder.Services.AddWristbandM2MAuth("auth02", options =>
{
    var m2mAuth02Config = builder.Configuration.GetSection("WristbandM2MAuthConfig:auth02");
    options.WristbandApplicationDomain = m2mAuth02Config["WristbandApplicationDomain"];
    options.ClientId = m2mAuth02Config["ClientId"];
    options.ClientSecret = m2mAuth02Config["ClientSecret"];
});

...
```

### Initialize Access Token Cache

Initialize the Wristband client to get the initial token during startup.

**Default Singleton Service**
```csharp
// Program.cs

...

try
{
    // Load the access token into the cache
    var wristbandM2MAuth = app.Services.GetRequiredService<IWristbandM2MAuthService>();
    await wristbandM2MAuth.GetTokenAsync();
}
catch (Exception ex)
{
    Console.WriteLine("Failed to retrieve initial M2M token: " + ex);
}

app.Run();
```

**Named Services**
```csharp
// Program.cs

...

try
{
    // Load the access token into the cache
    var serviceFactory = app.Services.GetRequiredService<WristbandM2MAuthServiceFactory>();
    var wristbandM2MAuth = serviceFactory.GetService("auth01");
    await wristbandM2MAuth.GetTokenAsync();
}
catch (Exception ex)
{
    Console.WriteLine("[M2M AUTH] Failed to retrieve initial M2M token: " + ex);
}

app.Run();
```

## 5) Inject M2M Auth into Your HTTP Client

You can use dependency injection to provide an instance of `WristbandM2MAuthService` to any HTTP client, allowing you to retrieve access tokens for authenticated downstream requests. If a token becomes invalid and usage results in unauthorized errors, you should clear the cached token to prevent repeated failures.

**Default Singleton Service**
```csharp
// ProtectedApiClient.cs
using System.Net;
using System.Net.Http.Headers;
using Wristband.AspNet.Auth.M2M;

public class ProtectedApiClient
{
    private readonly HttpClient _client;
    private readonly IWristbandM2MAuthService _wristbandM2MAuth;

    public ProtectedApiClient(HttpClient client, IWristbandM2MAuthService wristbandM2MAuth)
    {
        _client = client;
        _client.BaseAddress = new Uri("http://localhost:8080");
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Wristband M2M Auth
        _wristbandM2MAuth = wristbandM2MAuth;
    }

    public async Task<ResponseDto> GetProtectedDataAsync()
    {
        // Get the token from the M2M client (may refresh token if expired)
        var token = await _wristbandM2MAuth.GetTokenAsync();

        // Attach Bearer token to request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/protected/data");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Call the Protected API
        var response = await _client.SendAsync(request);

        // Clear the token cache for any unauthorized errors
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _wristbandM2MAuth.ClearToken();
        }

        // Ensure success and return response data
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResponseDto>();
    }
}
```

**Named Services**
```csharp
// ProtectedApiClient.cs
using System.Net;
using System.Net.Http.Headers;
using Wristband.AspNet.Auth.M2M;

public class ProtectedApiClient
{
    private readonly HttpClient _client;
    private readonly IWristbandM2MAuthService _wristbandM2MAuth;

    public ProtectedApiClientWithFactory(HttpClient client, WristbandM2MAuthServiceFactory authServiceFactory)
    {
        _client = client;
        _client.BaseAddress = new Uri("http://localhost:8080");
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Wristband M2M Auth - retrieve the specific named service
        _wristbandM2MAuth = authServiceFactory.GetService("auth01");
    }

    public async Task<ResponseDto> GetProtectedDataAsync()
    {
        // Get the token from the M2M client (may refresh token if expired)
        var token = await _wristbandM2MAuth.GetTokenAsync();

        // Attach Bearer token to request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/protected/data");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Call the Protected API
        var response = await _client.SendAsync(request);

        // Clear the token cache for any unauthorized errors
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _wristbandM2MAuth.ClearToken();
        }

        // Ensure success and return response data
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResponseDto>();
    }
}
```

## Token Caching and Background Refresh

The SDK automatically caches tokens in memory. When a token is nearing its expiration, the SDK will attempt to refresh it silently in the background, eliminating the need for manual refresh logic.

The SDK uses a short buffer between the time the token expires and when it attempts to refresh the token. You can adjust this buffer by passing an optional `TokenExpiryBuffer` value during SDK configuration:

```csharp
// Program.cs
builder.Services.AddWristbandM2MAuth(options =>
{
    ...

    // Refresh 60 seconds before actual expiry
    options.TokenExpiryBuffer = TimeSpan.FromMinutes(5);
});
```

You can also set an optional `BackgroundTokenRefreshInterval` to automatically refresh the access token at fixed intervals in the background:

```csharp
// Program.cs
builder.Services.AddWristbandM2MAuth(options =>
{
    ...

    // Refresh token every 15 minutes in the background
    options.BackgroundTokenRefreshInterval = TimeSpan.FromMinutes(15);
});
```

<br>

## SDK Configuration Options

| M2M Auth Option | Type | Required | Description |
| --------------- | ---- | -------- | ----------- |
| BackgroundTokenRefreshInterval | `TimeSpan` | No | Specifies how often the background process should attempt to refresh the access token. If not set, background refreshing is disabled. The minimum interval is 1 minute. |
| ClientId | string | Yes | The client ID of the Wristband M2M OAuth2 Client. |
| ClientSecret | string | Yes | The client secret of the Wristband M2M OAuth2 Client. |
| TokenExpiryBuffer | `TimeSpan` | No | Optional buffer time to subtract from the token’s expiration to ensure early refresh. Defaults to 60 seconds. Minimum is `TimeSpan.Zero`. |
| WristbandApplicationDomain | string | Yes | The vanity domain of the Wristband application. |

## API

The `IWristbandM2MAuthService` interface provides methods for retrieving and managing access tokens for M2M authentication.


### GetTokenAsync()

This method handles the process of fetching a new token or returning a cached one when it is still valid.

```csharp
var token = await wristbandM2MAuthService.GetTokenAsync();
```

### ClearToken

This method clears the currently cached access token, forcing the next request to retrieve a fresh token.

```csharp
wristbandM2MAuthService.ClearToken();
```

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this SDK.

<br/>
