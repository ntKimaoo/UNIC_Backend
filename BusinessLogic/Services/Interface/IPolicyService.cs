using BusinessLogic.DTOs;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IPolicyService
    {
        Task<IEnumerable<Policy>> GetUserPoliciesAsync(Guid userId);
        Task<bool> HasUserPolicyAsync(Guid userId, string policyTitle);
        Task<IEnumerable<PolicyGroup>> GetAllPolicyGroupAsync();
        Task<IEnumerable<Policy>> GetAllPoliciesByGroupAsync(int groupId);

        /// <summary>Lấy policies được gán trực tiếp (không qua role)</summary>
        Task<IEnumerable<PolicyResponseDto>> GetUserDirectPoliciesAsync(Guid userId);

        /// <summary>Gán thêm policies cho member (bỏ qua nếu đã có)</summary>
        Task AssignPoliciesToUserAsync(Guid userId, IEnumerable<int> policyIds);

        /// <summary>Thu hồi một policy khỏi member</summary>
        Task<bool> RevokePolicyFromUserAsync(Guid userId, int policyId);

        /// <summary>Ghi đè toàn bộ policies trực tiếp của member</summary>
        Task SetUserPoliciesAsync(Guid userId, IEnumerable<int> policyIds);

        /// <summary>Kiểm tra user có policy trong một club cụ thể không</summary>
        Task<bool> HasMemberPolicyInClubAsync(Guid userId, int clubId, string policyTitle);
    }
}
