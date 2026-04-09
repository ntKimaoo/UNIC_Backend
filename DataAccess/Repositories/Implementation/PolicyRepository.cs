using DataAccess.Models;
using UNIC.DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DataAccess.Repositories.Implementation
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly UnicContext _context;

        public PolicyRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId)
        {
            // Get policies from user's club roles
            var clubRolePolicies = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubRole.ClubRolePolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();
            // Get policies from club member's policies
            var clubMemberPolicies = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubMemberPolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();
            // Combine and return distinct policies
            return clubRolePolicies
                .Concat(clubMemberPolicies)
                .DistinctBy(p => p.Id)
                .ToList();
        }

        public async Task<Policy?> GetPolicyByTitleAsync(string title)
        {
            return await _context.Policies
                .FirstOrDefaultAsync(p => p.Title == title);
        }

        public async Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle)
        {
            // Check club member-based policy assignment
            var hasClubMemberPolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubMemberPolicies)
                .AnyAsync(crp => crp.Policy.Name.ToLower().Trim() == policyTitle);
            return hasClubMemberPolicy;
        }

        public async Task<IEnumerable<PolicyGroup>> GetAllPolicyGroupAsync()
        {
            return await _context.PolicyGroups.ToListAsync();
        }
        public async Task<IEnumerable<Policy>> GetAllPoliciesByGroupAsync(int groupId)
        {
            return await _context.Policies
                .Where(p => p.PolicyGroupId == groupId)
                .ToListAsync();
        }
        public async Task<bool> HasMemberPolicyInClubAsync(Guid userId, int clubId, string policyTitle)
        {
            var normalizedTitle = policyTitle.ToLower().Trim();
            var isClubManager = await _context.UserClubRoles.AnyAsync(ucr => ucr.UserId == userId && ucr.ClubRole.Level == 0);
            if (isClubManager) return true;
            // Check policy from club role (within this specific club)
            var hasRolePolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId && ucr.ClubId == clubId)
                .SelectMany(ucr => ucr.ClubRole!.ClubRolePolicies)
                .AnyAsync(crp => crp.Policy.Name == normalizedTitle);

            if (hasRolePolicy) return true;

            var hasMemberPolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId && ucr.ClubId == clubId)
                .SelectMany(ucr => ucr.ClubMemberPolicies!)
                .AnyAsync(cmp => cmp.Policy.Name == normalizedTitle);

            return hasMemberPolicy;
        }

        public async Task<IEnumerable<Policy>> GetUserDirectPoliciesAsync(Guid userId)
        {
            return await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubMemberPolicies)
                .Select(cmp => cmp.Policy)
                .Distinct()
                .ToListAsync();
        }

        public async Task AssignPoliciesToUserAsync(Guid userId, IEnumerable<int> policyIds)
        {
            var userRoles = await _context.UserClubRoles
                .Include(ucr => ucr.ClubMemberPolicies)
                .Where(ucr => ucr.UserId == userId)
                .ToListAsync();

            if (!userRoles.Any()) return;

            foreach (var userRole in userRoles)
            {
                var existingPolicyIds = userRole.ClubMemberPolicies?.Select(p => p.PolicyId).ToHashSet() ?? new HashSet<int>();
                foreach (var policyId in policyIds)
                {
                    if (!existingPolicyIds.Contains(policyId))
                    {
                        var cmp = new ClubMemberPolicy
                        {
                            ClubMemberId = userRole.ClubMemberId,
                            PolicyId = policyId
                        };
                        _context.Add(cmp);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RevokePolicyFromUserAsync(Guid userId, int policyId)
        {
            var userRoles = await _context.UserClubRoles
                .Include(ucr => ucr.ClubMemberPolicies)
                .Where(ucr => ucr.UserId == userId)
                .ToListAsync();

            if (!userRoles.Any()) return false;

            var removedAny = false;
            foreach (var userRole in userRoles)
            {
                var policyToRemove = userRole.ClubMemberPolicies?.FirstOrDefault(p => p.PolicyId == policyId);
                if (policyToRemove != null)
                {
                    _context.Remove(policyToRemove);
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                await _context.SaveChangesAsync();
            }

            return removedAny;
        }

        public async Task SetUserPoliciesAsync(Guid userId, IEnumerable<int> policyIds)
        {
            var userRoles = await _context.UserClubRoles
                .Include(ucr => ucr.ClubMemberPolicies)
                .Where(ucr => ucr.UserId == userId)
                .ToListAsync();

            if (!userRoles.Any()) return;

            foreach (var userRole in userRoles)
            {
                if (userRole.ClubMemberPolicies != null)
                {
                    _context.RemoveRange(userRole.ClubMemberPolicies);
                }

                foreach (var policyId in policyIds)
                {
                    var cmp = new ClubMemberPolicy
                    {
                        ClubMemberId = userRole.ClubMemberId,
                        PolicyId = policyId
                    };
                    _context.Add(cmp);
                }
            }

            await _context.SaveChangesAsync();
        }
        public async Task<List<string>> GetUserRoleAsync(Guid userId)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleName)
                .ToListAsync();
            return userRoles;
        }
    }
}
