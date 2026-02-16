using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Dynamic policy provider that creates authorization policies on-the-fly
    /// </summary>
    public class DynamicPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
        private const string POLICY_PREFIX = "Policy_";

        public DynamicPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if this is a dynamic policy (starts with our prefix)
            if (policyName.StartsWith(POLICY_PREFIX))
            {
                // Extract the actual policy title
                var policyTitle = policyName.Substring(POLICY_PREFIX.Length);

                // Build a policy with our custom requirement
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PolicyRequirement(policyTitle))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // Fall back to default provider for non-dynamic policies
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
