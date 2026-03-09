using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

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
        Task <IEnumerable<Policy>> GetPoliciesByRoleAsync(int clubRoleId);
        Task<IEnumerable<ClubRole>> GetRolesByClubIdAsync(int clubId);
    }
}
