using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IPolicyService
    {
        /// <summary>
        /// Get all policies assigned to a user
        /// </summary>
        Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId);

        /// <summary>
        /// Check if a user has a specific policy
        /// </summary>
        Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle);

        /// <summary>
        /// Get all available policies
        /// </summary>
        Task<IEnumerable<PolicyGroup>> GetAllPolicyGroupAsync();
        Task<IEnumerable<Policy>> GetAllPoliciesByGroupAsync(int groupId);
    }
}
