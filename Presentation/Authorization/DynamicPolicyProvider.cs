using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Dynamic policy provider that creates authorization policies on-the-fly.
    ///
    /// Supported policy name prefixes
    /// ──────────────────────────────────────────────────────────────────────────
    ///   ClubPolicy_&lt;policyTitle&gt;
    ///       → checks HasMemberPolicyInClubAsync(userId, clubId, policyTitle)
    ///         (handler: ClubPolicyOrRoleHandler with PolicyTitle set, no Roles)
    ///
    ///   Role_&lt;role1&gt;[,&lt;role2&gt;,...]
    ///       → checks whether the user's "UserRole" claim matches any listed role
    ///         (handler: ClubPolicyOrRoleHandler with Roles set, no PolicyTitle)
    ///
    ///   ClubPolicyOrRole_&lt;policyTitle&gt;|&lt;role1&gt;[,&lt;role2&gt;,...]
    ///       → OR logic: succeeds when either the role check or the club-policy
    ///         check passes
    ///         (handler: ClubPolicyOrRoleHandler with both fields set)
    /// </summary>
    public class DynamicPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        private const string CLUB_POLICY_PREFIX     = "ClubPolicy_";
        private const string ROLE_PREFIX            = "Role_";
        private const string COMBINED_PREFIX        = "ClubPolicyOrRole_";

        public DynamicPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // ── ClubPolicyOrRole_<policyTitle>|<role1>,<role2>  ──────────────────
            if (policyName.StartsWith(COMBINED_PREFIX))
            {
                var rest  = policyName.Substring(COMBINED_PREFIX.Length);
                // Format: <policyTitle>|<role1>,<role2>,...
                var pipe  = rest.IndexOf('|');
                var policyTitle = pipe >= 0 ? rest.Substring(0, pipe) : rest;
                var roles       = pipe >= 0
                    ? rest.Substring(pipe + 1).Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    : System.Array.Empty<string>();

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ClubPolicyOrRoleRequirement(policyTitle, roles))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // ── ClubPolicy_<policyTitle>  ─────────────────────────────────────────
            if (policyName.StartsWith(CLUB_POLICY_PREFIX))
            {
                var policyTitle = policyName.Substring(CLUB_POLICY_PREFIX.Length);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ClubPolicyOrRoleRequirement(policyTitle, System.Array.Empty<string>()))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // ── Role_<role1>,<role2>,...  ─────────────────────────────────────────
            if (policyName.StartsWith(ROLE_PREFIX))
            {
                var roles = policyName.Substring(ROLE_PREFIX.Length)
                                      .Split(',', System.StringSplitOptions.RemoveEmptyEntries);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ClubPolicyOrRoleRequirement(null, roles))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
