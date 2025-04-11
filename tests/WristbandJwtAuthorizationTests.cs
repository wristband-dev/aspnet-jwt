using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Wristband.AspNet.Auth.Jwt.Tests;

public class WristbandJwtAuthorizationTests
{
    [Fact]
    public void PolicyName_HasCorrectValue()
    {
        Assert.Equal("WristbandJwt", WristbandJwtAuthorization.PolicyName);
    }

    [Fact]
    public void GetPolicy_ReturnsNonNullPolicy()
    {
        var policy = WristbandJwtAuthorization.GetPolicy();

        Assert.NotNull(policy);
    }

    [Fact]
    public void GetPolicy_ConfiguresJwtBearerScheme()
    {
        var policy = WristbandJwtAuthorization.GetPolicy();

        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, policy.AuthenticationSchemes);
    }

    [Fact]
    public void GetPolicy_RequiresAuthenticatedUser()
    {
        var policy = WristbandJwtAuthorization.GetPolicy();

        Assert.Contains(policy.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void GetPolicy_ReturnsNewPolicyInstance_OnEachCall()
    {
        var policy1 = WristbandJwtAuthorization.GetPolicy();
        var policy2 = WristbandJwtAuthorization.GetPolicy();

        Assert.NotSame(policy1, policy2);
    }

    [Fact]
    public void GetPolicy_ReturnsConsistentPolicies_AcrossCalls()
    {
        var policy1 = WristbandJwtAuthorization.GetPolicy();
        var policy2 = WristbandJwtAuthorization.GetPolicy();

        Assert.Equal(policy1.AuthenticationSchemes.Count, policy2.AuthenticationSchemes.Count);
        Assert.Equal(policy1.Requirements.Count, policy2.Requirements.Count);

        // Verify same authentication scheme
        foreach (var scheme in policy1.AuthenticationSchemes)
        {
            Assert.Contains(scheme, policy2.AuthenticationSchemes);
        }

        // Verify same requirement types
        foreach (var requirement in policy1.Requirements)
        {
            Assert.Contains(policy2.Requirements, r => r.GetType() == requirement.GetType());
        }
    }
}
