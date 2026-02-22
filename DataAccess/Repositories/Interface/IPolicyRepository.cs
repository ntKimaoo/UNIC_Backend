using DataAccess.Models;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IPolicyRepository
    {
        /// <summary>
        /// Get all policies assigned to a user (both direct and role-based)
        /// </summary>
        Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId);

        /// <summary>
        /// Get a policy by its title
        /// </summary>
        Task<Policy?> GetPolicyByTitleAsync(string title);

        /// <summary>
        /// Check if a user has a specific policy
        /// </summary>
        Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle);

        /// <summary>
        /// Get all policies
        /// </summary>
        Task<IEnumerable<Policy>> GetAllPoliciesAsync();
    }
}
