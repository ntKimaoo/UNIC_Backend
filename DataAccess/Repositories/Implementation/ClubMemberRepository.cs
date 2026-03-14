using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class ClubMemberRepository : IClubMemberRepository
    {
        private readonly UnicContext _context;

        public ClubMemberRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserClubRole>> GetMembersByClubIdAsync(int clubId)
        {
            return await _context.UserClubRoles
                .Include(m => m.User)
                .Include(m => m.ClubRole)
                .Where(m => m.ClubId == clubId)
                .OrderBy(m => m.JoinDate)
                .ToListAsync();
        }

        public async Task<UserClubRole?> GetMemberByIdAsync(int clubMemberId)
        {
            return await _context.UserClubRoles
                .Include(m => m.User)
                .Include(m => m.ClubRole)
                .FirstOrDefaultAsync(m => m.ClubMemberId == clubMemberId);
        }

        public async Task<UserClubRole?> GetMemberAsync(Guid userId, int clubId)
        {
            return await _context.UserClubRoles
                .Include(m => m.User)
                .Include(m => m.ClubRole)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.ClubId == clubId);
        }

        public async Task<UserClubRole> AddMemberAsync(UserClubRole member)
        {
            await _context.UserClubRoles.AddAsync(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task<bool> UpdateMemberAsync(UserClubRole member)
        {
            try
            {
                _context.UserClubRoles.Update(member);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveMemberAsync(int clubMemberId)
        {
            try
            {
                var member = await _context.UserClubRoles.FindAsync(clubMemberId);
                if (member == null) return false;

                _context.UserClubRoles.Remove(member);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsMemberAsync(Guid userId, int clubId)
        {
            return await _context.UserClubRoles
                .AnyAsync(m => m.UserId == userId && m.ClubId == clubId);
        }

        public async Task<IEnumerable<UserClubRole>> GetClubsByUserIdAsync(Guid userId)
        {
            return await _context.UserClubRoles
                .Include(m => m.Club)
                .Include(m => m.ClubRole)
                .Include(m => m.User)
                    .ThenInclude(u => u.DepartmentMembers)
                    .ThenInclude(dm => dm.Department)
                .Include(m => m.User)
                    .ThenInclude(u => u.DepartmentMembers)
                    .ThenInclude(dm => dm.DepartmentRole)
                .Where(m => m.UserId == userId && string.Equals(m.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.JoinDate)
                .ToListAsync();
        }
    }
}
