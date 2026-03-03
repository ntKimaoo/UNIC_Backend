using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            => await _policyRepository.GetUserPoliciesAsync(userId);

        public async Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle)
            => await _policyRepository.HasUserPolicyAsync(userId, policyTitle);

        public async Task<IEnumerable<PolicyGroup>> GetAllPolicyGroupAsync()
            => await _policyRepository.GetAllPolicyGroupAsync();

        public async Task<IEnumerable<Policy>> GetAllPoliciesByGroupAsync(int groupId)
            => await _policyRepository.GetAllPoliciesByGroupAsync(groupId);

        public async Task<IEnumerable<PolicyResponseDto>> GetMemberDirectPoliciesAsync(Guid userId)
        {
            var policies = await _policyRepository.GetMemberDirectPoliciesAsync(userId);
            return policies.Select(p => new PolicyResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description
            });
        }

        public async Task AssignPoliciesToMemberAsync(Guid userId, IEnumerable<int> policyIds)
            => await _policyRepository.AssignPoliciesToMemberAsync(userId, policyIds);

        public async Task<bool> RevokePolicyFromMemberAsync(Guid userId, int policyId)
            => await _policyRepository.RevokePolicyFromMemberAsync(userId, policyId);

        public async Task SetMemberPoliciesAsync(Guid userId, IEnumerable<int> policyIds)
            => await _policyRepository.SetMemberPoliciesAsync(userId, policyIds);
    }
}