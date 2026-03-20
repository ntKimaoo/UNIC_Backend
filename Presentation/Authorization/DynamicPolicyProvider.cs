using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Dynamic policy provider that creates authorization policies on-the-fly.
    /// Supports two prefixes:
    ///   Policy_     → global user-level check (PolicyRequirement + PolicyAuthorizationHandler)
    ///   ClubPolicy_ → club-scoped check     (ClubMemberRequirement + ClubMemberAuthorizationHandler)
    /// </summary>
    public class DynamicPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
        private const string POLICY_PREFIX = "Policy_";
        private const string CLUB_POLICY_PREFIX = "ClubPolicy_";

        public DynamicPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Club-scoped policy: ClubPolicy_<policyTitle>
            if (policyName.StartsWith(CLUB_POLICY_PREFIX))
            {
                var policyTitle = policyName.Substring(CLUB_POLICY_PREFIX.Length);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ClubMemberRequirement(policyTitle))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // Global user-level policy: Policy_<policyTitle>
            if (policyName.StartsWith(POLICY_PREFIX))
            {
                var policyTitle = policyName.Substring(POLICY_PREFIX.Length);

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
