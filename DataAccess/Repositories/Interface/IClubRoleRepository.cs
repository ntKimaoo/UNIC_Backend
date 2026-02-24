using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IClubRoleRepository
    {
        Task<ClubRole?> GetByIdAsync(int clubRoleId);
        Task<IEnumerable<ClubRole>> GetAllAsync();
        Task<bool> RoleNameExistsAsync(string roleName);
        Task<ClubRole> CreateAsync(ClubRole clubRole);
        Task<bool> UpdateAsync(ClubRole clubRole);
        Task<bool> DeleteAsync(int clubRoleId);
        Task SetPoliciesAsync(int clubRoleId, IEnumerable<int> policyIds);
    }
}
