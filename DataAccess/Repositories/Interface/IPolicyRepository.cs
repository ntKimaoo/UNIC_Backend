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
        Task<IEnumerable<Policy>> GetMemberDirectPoliciesAsync(Guid userId);

        /// <summary>Gán danh sách policies cho member (bỏ qua nếu đã tồn tại)</summary>
        Task AssignPoliciesToMemberAsync(Guid userId, IEnumerable<int> policyIds);

        /// <summary>Thu hồi một policy khỏi member</summary>
        Task<bool> RevokePolicyFromMemberAsync(Guid userId, int policyId);

        /// <summary>Ghi đè toàn bộ policies của member (thay thế hết)</summary>
        Task SetMemberPoliciesAsync(Guid userId, IEnumerable<int> policyIds);
    }
}
