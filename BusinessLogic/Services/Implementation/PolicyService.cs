using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;

        public PolicyService(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId)
        {
            return await _policyRepository.GetUserPoliciesAsync(userId);
        }

        public async Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle)
        {
            return await _policyRepository.HasUserPolicyAsync(userId, policyTitle);
        }

        public async Task<IEnumerable<Policy>> GetAllPoliciesAsync()
        {
            return await _policyRepository.GetAllPoliciesAsync();
        }
    }
}
