using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

namespace DataAccess.Repositories.Implementation
{
    public class ClubRoleRepository : IClubRoleRepository
    {
        private readonly UnicContext _context;

        public ClubRoleRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<ClubRole?> GetByIdAsync(int clubRoleId, int? clubId)
        {
            return await _context.ClubRoles
                .Include(cr => cr.ClubMembers)
                .Include(cr => cr.ClubRolePolicies!)
                    .ThenInclude(crp => crp.Policy).ThenInclude(p => p.PolicyGroup)
                .FirstOrDefaultAsync(cr => cr.ClubRoleId == clubRoleId && cr.ClubId == clubId);
        }

        public async Task<IEnumerable<ClubRole>> GetAllAsync(int clubId)
        {
            return await _context.ClubRoles
                .Where(cr => cr.ClubId == clubId)
                .Include(cr => cr.ClubMembers)
                .Include(cr => cr.ClubRolePolicies!)
                    .ThenInclude(crp => crp.Policy).ThenInclude(p => p.PolicyGroup)
                .OrderBy(cr => cr.Level)
                .ToListAsync();
        }

        public async Task<bool> RoleNameExistsAsync(string roleName,int? clubId)
        {
            return await _context.ClubRoles.Where(cr=>cr.ClubId==clubId)
                .AnyAsync(cr => cr.RoleName == roleName);
        }

        public async Task<ClubRole> CreateAsync(ClubRole clubRole)
        {
            await _context.ClubRoles.AddAsync(clubRole);
            await _context.SaveChangesAsync();
            return clubRole;
        }

        public async Task<bool> UpdateAsync(ClubRole clubRole)
        {
            try
            {
                _context.ClubRoles.Update(clubRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int clubRoleId)
        {
            try
            {
                await _context.UserClubRoles
                     .Where(cm => cm.ClubRoleId == clubRoleId)
                     .ExecuteUpdateAsync(setters =>
                         setters.SetProperty(cm => cm.ClubRoleId, (int?)null));
                var clubRole = await _context.ClubRoles.FindAsync(clubRoleId);
                if (clubRole == null)
                    return false;
                await _context.ClubRolePolicies
                    .Where(rp => rp.ClubRoleId == clubRoleId)
                    .ExecuteDeleteAsync();
                _context.ClubRoles.Remove(clubRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetPoliciesAsync(int clubRoleId, IEnumerable<int> policyIds)
        {
            // Xóa toàn bộ policies hiện tại của role này
            var existing = await _context.ClubRolePolicies
                .Where(crp => crp.ClubRoleId == clubRoleId)
                .ToListAsync();

            _context.ClubRolePolicies.RemoveRange(existing);

            // Thêm policies mới
            foreach (var id in policyIds)
            {
                await _context.ClubRolePolicies.AddAsync(
               new ClubRolePolicy
                {
                    ClubRoleId = clubRoleId,
                    PolicyId = id
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ClubRole>> GetByDepartmentIdAsync(int departmentId)
        {
            return await _context.ClubRoles
                .Where(cr => cr.DepartmentId == departmentId)
                .ToListAsync();
        }
    }
}
