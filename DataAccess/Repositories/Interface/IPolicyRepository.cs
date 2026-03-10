using DataAccess.Models;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IPolicyRepository
    {
        Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId);
        Task<Policy?> GetPolicyByTitleAsync(string title);
        Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle);
        Task<IEnumerable<PolicyGroup>> GetAllPolicyGroupAsync();
        Task<IEnumerable<Policy>> GetAllPoliciesByGroupAsync(int groupId);
    }
}
