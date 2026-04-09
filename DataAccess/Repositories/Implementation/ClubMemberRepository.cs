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

        public async Task<(IEnumerable<UserClubRole> Items, int TotalCount)> GetMembersByClubIdAsync(
            int clubId, int? pagination, int? page, string? filter, bool? ascending, string? sortBy)
        {
            var query = _context.UserClubRoles
                .Include(m => m.User)
                .Include(m => m.ClubRole)
                .Where(m => m.ClubId == clubId)
                .AsQueryable();

            // Filter
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var keyword = filter.Trim().ToLower();
                query = query.Where(m =>
                    (m.User != null && m.User.FullName != null && m.User.FullName.ToLower().Contains(keyword)) ||
                    (m.User != null && m.User.Email != null && m.User.Email.ToLower().Contains(keyword)) ||
                    (m.User != null && m.User.StudentId != null && m.User.StudentId.ToLower().Contains(keyword)) ||
                    (m.ClubRole != null && m.ClubRole.RoleName != null && m.ClubRole.RoleName.ToLower().Contains(keyword)) ||
                    (m.Status != null && m.Status.ToLower().Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            // Sort
            bool isAscending = ascending ?? true;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                query = sortBy.Trim().ToLower() switch
                {
                    "fullname" => isAscending ? query.OrderBy(m => m.User!.FullName) : query.OrderByDescending(m => m.User!.FullName),
                    "email" => isAscending ? query.OrderBy(m => m.User!.Email) : query.OrderByDescending(m => m.User!.Email),
                    "studentid" => isAscending ? query.OrderBy(m => m.User!.StudentId) : query.OrderByDescending(m => m.User!.StudentId),
                    "rolename" => isAscending ? query.OrderBy(m => m.ClubRole!.RoleName) : query.OrderByDescending(m => m.ClubRole!.RoleName),
                    "joindate" => isAscending ? query.OrderBy(m => m.JoinDate) : query.OrderByDescending(m => m.JoinDate),
                    "status" => isAscending ? query.OrderBy(m => m.Status) : query.OrderByDescending(m => m.Status),
                    _ => query.OrderBy(m => m.JoinDate) // Default fallback
                };
            }
            else
            {
                // Default sort if none provided
                query = isAscending ? query.OrderBy(m => m.JoinDate) : query.OrderByDescending(m => m.JoinDate);
            }

            // Pagination
            if (pagination.HasValue && pagination.Value > 0)
            {
                int currentPage = page ?? 1;
                int pageSize = pagination.Value;
                query = query.Skip((currentPage - 1) * pageSize).Take(pageSize);
            }

            var items = await query.ToListAsync();
            return (items, totalCount);
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
                .Where(m => m.UserId == userId && m.Status != null && m.Status.ToUpper() == "ACTIVE")
                .OrderBy(m => m.JoinDate)
                .ToListAsync();
        }
    }
}
