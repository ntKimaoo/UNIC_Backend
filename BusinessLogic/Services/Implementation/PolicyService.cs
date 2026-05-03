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

        public async Task<IEnumerable<PolicyResponseDto>> GetUserDirectPoliciesAsync(Guid userId)
        {
            var policies = await _policyRepository.GetUserDirectPoliciesAsync(userId);
            return policies.Select(p => new PolicyResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description
            });
        }

        public async Task AssignPoliciesToUserAsync(Guid userId, IEnumerable<int> policyIds)
            => await _policyRepository.AssignPoliciesToUserAsync(userId, policyIds);

        public async Task<bool> RevokePolicyFromUserAsync(Guid userId, int policyId)
            => await _policyRepository.RevokePolicyFromUserAsync(userId, policyId);

        public async Task SetUserPoliciesAsync(Guid userId, IEnumerable<int> policyIds)
            => await _policyRepository.SetUserPoliciesAsync(userId, policyIds);

        public async Task<bool> HasMemberPolicyInClubAsync(Guid userId, int clubId, string policyTitle)
            => await _policyRepository.HasMemberPolicyInClubAsync(userId, clubId, policyTitle);

           //lay list role 
        public async Task<List<string>> GetUserRole(Guid userId)
            => await _policyRepository.GetUserRoleAsync(userId);

        // public async Task<string> GetUserRole(Guid userId)
        //     => await _policyRepository.GetUserRoleAsync(userId);

        public async Task<IEnumerable<string>> GetPoliciesInClubAsync(Guid userId, int clubId)
            => await _policyRepository.GetPoliciesInClubAsync(userId, clubId);
    }
}
