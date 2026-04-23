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

        /// <summary>Lấy policies được gán trực tiếp cho member (không qua role)</summary>
        Task<IEnumerable<Policy>> GetUserDirectPoliciesAsync(Guid userId);

        /// <summary>Gán danh sách policies cho member (bỏ qua nếu đã tồn tại)</summary>
        Task AssignPoliciesToUserAsync(Guid userId, IEnumerable<int> policyIds);

        /// <summary>Thu hồi một policy khỏi member</summary>
        Task<bool> RevokePolicyFromUserAsync(Guid userId, int policyId);

        /// <summary>Ghi đè toàn bộ policies của member (thay thế hết)</summary>
        Task SetUserPoliciesAsync(Guid userId, IEnumerable<int> policyIds);

        /// <summary>Kiểm tra user có policy trong một club cụ thể không (qua club role hoặc direct member policy)</summary>
        Task<bool> HasMemberPolicyInClubAsync(Guid userId, int clubId, string policyTitle);
        Task<string> GetUserRoleAsync(Guid userId);
    }
}
