# Wristband Jwt Validation SDK for ASP.NET

Wristband provides enterprise-ready auth that is secure by default, truly multi-tenant, and ungated for small businesses.

- Website: [Wristband Website](https://wristband.dev)
- Documentation: [Wristband Docs](https://docs.wristband.dev/)

For detailed setup instructions and usage guidelines, visit the project's GitHub repository.

- [ASP.NET JWT SDK - GitHub](https://github.com/wristband-dev/aspnet-jwt)

## Details

This ASP.NET Core SDK validates JWT access tokens issued by Wristband for user or machine authentication. It uses the Wristband JWKS endpoint to resolve signing keys and verify RS256 signatures. Validation includes issuer verification, lifetime checks, and signature validation using cached keys. Supported on .NET 6 or later.

The SDK integrates with ASP.NET Core's built-in authentication and authorization system. Once configured, it enables authorization policies to be enforced at the endpoint level using `RequireAuthorization`. This allows developers to declaratively protect routes and ensure that only valid, Wristband-issued access tokens can access secured APIs.

You can learn more about JWTs in Wristband in our documentation:

- [JWTs and Signing Keys](https://docs.wristband.dev/docs/json-web-tokens-jwts-and-signing-keys)

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this SDK.
