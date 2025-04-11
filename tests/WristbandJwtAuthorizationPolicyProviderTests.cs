using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

using Xunit;

namespace Wristband.AspNet.Auth.Jwt.Tests
{
    public class WristbandJwtAuthorizationPolicyProviderTests
    {
        [Fact]
        public void Configure_AddsPolicy_WithCorrectName()
        {
            var options = new AuthorizationOptions();
            var provider = new WristbandJwtAuthorizationPolicyProvider();

            provider.Configure(options);

            var policy = options.GetPolicy(WristbandJwtAuthorization.PolicyName);
            Assert.NotNull(policy);
        }

        [Fact]
        public void Configure_Policy_RequiresJwtBearerScheme()
        {
            var options = new AuthorizationOptions();
            var provider = new WristbandJwtAuthorizationPolicyProvider();

            provider.Configure(options);

            var policy = options.GetPolicy(WristbandJwtAuthorization.PolicyName);
            Assert.Contains(JwtBearerDefaults.AuthenticationScheme, policy?.AuthenticationSchemes!);
        }

        [Fact]
        public void Configure_Policy_RequiresAuthenticatedUser()
        {
            var options = new AuthorizationOptions();
            var provider = new WristbandJwtAuthorizationPolicyProvider();

            provider.Configure(options);

            var policy = options.GetPolicy(WristbandJwtAuthorization.PolicyName);
            Assert.Contains(policy?.Requirements!, r => r is DenyAnonymousAuthorizationRequirement);
        }

        [Fact]
        public void Configure_DoesNotAffectExistingPolicies()
        {
            var options = new AuthorizationOptions();
            var existingPolicyName = "ExistingPolicy";
            options.AddPolicy(existingPolicyName, policy => policy.RequireRole("Admin"));
            var provider = new WristbandJwtAuthorizationPolicyProvider();
            provider.Configure(options);

            var existingPolicy = options.GetPolicy(existingPolicyName);
            Assert.NotNull(existingPolicy);
            Assert.Contains(existingPolicy.Requirements, r => r is RolesAuthorizationRequirement);
        }

        [Fact]
        public void Configure_WithMultipleCalls_UpdatesExistingPolicy()
        {
            var options = new AuthorizationOptions();
            var provider = new WristbandJwtAuthorizationPolicyProvider();

            // Configure once
            provider.Configure(options);

            // Get initial config count
            var initialPolicy = options.GetPolicy(WristbandJwtAuthorization.PolicyName);
            var initialRequirementsCount = initialPolicy?.Requirements.Count;

            // Configure again
            provider.Configure(options);

            // Policy should be updated, not duplicated
            var updatedPolicy = options.GetPolicy(WristbandJwtAuthorization.PolicyName);
            Assert.Equal(initialRequirementsCount, updatedPolicy?.Requirements.Count);
        }
    }
}
