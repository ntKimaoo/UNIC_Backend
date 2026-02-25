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

        public async Task<ClubRole?> GetByIdAsync(int clubRoleId)
        {
            return await _context.ClubRoles
                .Include(cr => cr.ClubMembers)
                .Include(cr => cr.ClubRolePolicies!)
                    .ThenInclude(crp => crp.Policy).ThenInclude(p => p.PolicyGroup)
                .FirstOrDefaultAsync(cr => cr.ClubRoleId == clubRoleId);
        }

        public async Task<IEnumerable<ClubRole>> GetAllAsync()
        {
            return await _context.ClubRoles
                .Include(cr => cr.ClubMembers)
                .Include(cr => cr.ClubRolePolicies!)
                    .ThenInclude(crp => crp.Policy).ThenInclude(p => p.PolicyGroup)
                .OrderBy(cr => cr.Level)
                .ToListAsync();
        }

        public async Task<bool> RoleNameExistsAsync(string roleName)
        {
            return await _context.ClubRoles
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
                var clubRole = await _context.ClubRoles.FindAsync(clubRoleId);
                if (clubRole == null)
                    return false;

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
        public async Task<IEnumerable<Policy>> GetPoliciesByRoleAsync(int groupId)
        {
            return await _context.Policies
            .Where(p => p.ClubRolePolicies
            .Any(crp => crp.ClubRoleId == groupId))
            .ToListAsync();
        }
    }
}
