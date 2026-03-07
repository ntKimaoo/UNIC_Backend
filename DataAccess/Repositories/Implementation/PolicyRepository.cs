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
            var directPolicies = await _context.ClubMemberPolicies
                .Where(cmp => cmp.UserId == userId)
                .Select(cmp => cmp.Policy)
                .ToListAsync();

            // Get policies from user's club roles
            var rolePolicies = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubRole.ClubRolePolicies)
                .Select(crp => crp.Policy)
                .ToListAsync();

            // Combine and return distinct policies
            return directPolicies
                .Concat(rolePolicies)
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
            var hasDirectPolicy = await _context.ClubMemberPolicies
                .AnyAsync(cmp => cmp.UserId == userId && cmp.Policy.Title == policyTitle);

            if (hasDirectPolicy)
                return true;

            // Check role-based policy assignment
            var hasRolePolicy = await _context.UserClubRoles
                .Where(ucr => ucr.UserId == userId)
                .SelectMany(ucr => ucr.ClubRole.ClubRolePolicies)
                .AnyAsync(crp => crp.Policy.Title == policyTitle);

            return hasRolePolicy;
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
    }
}