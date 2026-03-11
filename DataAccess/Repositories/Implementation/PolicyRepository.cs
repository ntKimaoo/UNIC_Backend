using DataAccess.Models;
using UNIC.DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // Get policies from direct user assignment
            var directUserPolicies = await _context.UserPolicies
                .Where(cmp => cmp.UserId == userId)
                .Select(cmp => cmp.Policy)
                .ToListAsync();

            // Get policies from user's club roles
            var clubRolePolicies = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubRole.ClubRolePolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();
            // Get policies from user's roles
            var userRolePolicies = await _context.UserRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.UserRolePolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();
            // Get policies from club member's policies
            var clubMemberPolicies = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubMemberPolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();
            // Combine and return distinct policies
            return directUserPolicies
                .Concat(clubRolePolicies)
                .Concat(userRolePolicies)
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
            // Check direct user policy assignment
            var hasDirectPolicy = await _context.UserPolicies
                .AnyAsync(cmp => cmp.UserId == userId && cmp.Policy.Title == policyTitle);

            if (hasDirectPolicy)
                return true;

            // Check club role-based policy assignment
            var hasClubRolePolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubRole.ClubRolePolicies)
                .AnyAsync(crp => crp.Policy.Title == policyTitle);

            if (hasClubRolePolicy) return true;
            // Check user role-based policy assignment
            var hasUserRolePolicy = await _context.UserRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.UserRolePolicies)
                .AnyAsync(crp => crp.Policy.Title == policyTitle);
            if (hasUserRolePolicy) return true;
            // Check club member-based policy assignment
            var hasClubMemberPolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubMemberPolicies)
                .AnyAsync(crp => crp.Policy.Title == policyTitle);
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

        public async Task<IEnumerable<Policy>> GetUserDirectPoliciesAsync(Guid userId)
        {
            return await _context.UserPolicies
                .Where(cmp => cmp.UserId == userId)
                .Include(cmp => cmp.Policy)
                .Select(cmp => cmp.Policy)
                .ToListAsync();
        }

        public async Task AssignPoliciesToUserAsync(Guid userId, IEnumerable<int> policyIds)
        {
            var existingIds = await _context.UserPolicies
                .Where(cmp => cmp.UserId == userId)
                .Select(cmp => cmp.PolicyId)
                .ToListAsync();

            var toAdd = policyIds.Distinct()
                .Where(id => !existingIds.Contains(id))
                .Select(id => new UserPolicy { UserId = userId, PolicyId = id });

            await _context.UserPolicies.AddRangeAsync(toAdd);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RevokePolicyFromUserAsync(Guid userId, int policyId)
        {
            var entry = await _context.UserPolicies
                .FirstOrDefaultAsync(cmp => cmp.UserId == userId && cmp.PolicyId == policyId);

            if (entry == null) return false;

            _context.UserPolicies.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SetUserPoliciesAsync(Guid userId, IEnumerable<int> policyIds)
        {
            var existing = await _context.UserPolicies
                .Where(cmp => cmp.UserId == userId)
                .ToListAsync();

            _context.UserPolicies.RemoveRange(existing);

            var newEntries = policyIds.Distinct()
                .Select(id => new UserPolicy { UserId = userId, PolicyId = id });

            await _context.UserPolicies.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();
        }
    }
}
