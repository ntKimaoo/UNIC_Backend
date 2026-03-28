using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

namespace DataAccess.Repositories.Interface
{
    public interface IClubRoleRepository
    {
        Task<ClubRole?> GetByIdAsync(int clubRoleId,int? clubId);
        Task<IEnumerable<ClubRole>> GetAllAsync(int clubId);
        Task<bool> RoleNameExistsAsync(string roleName,int? clubId);
        Task<ClubRole> CreateAsync(ClubRole clubRole);
        Task<bool> UpdateAsync(ClubRole clubRole);
        Task<bool> DeleteAsync(int clubRoleId);
        Task<IEnumerable<ClubRole>> GetByDepartmentIdAsync(int departmentId);
        Task SetPoliciesAsync(int clubRoleId, IEnumerable<int> policyIds);
        Task<UserClubRole?> GetUserClubRoleAsync(Guid userId, int clubId);
        Task<bool> AddUserClubRoleAsync(UserClubRole member);
        Task<bool> UpdateUserClubRoleAsync(UserClubRole member);
        Task<List<Club>> GetManagedClubsAsync(Guid userId);
        Task<List<int>> GetClubIdsWithFewRolesAsync(int threshold);
        Task<List<Guid>> GetManagerIdsForClubsAsync(List<int> clubIds);

    }
}
