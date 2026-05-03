using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class ClubRepository : IClubRepository
    {
        private readonly UnicContext _context;

        public ClubRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<Club?> GetByIdAsync(int clubId)
        {
            return await _context.Clubs
                .FirstOrDefaultAsync(c => c.ClubId == clubId && !c.IsDeleted);
        }

        public async Task<IEnumerable<Club>> GetAllAsync()
        {
            return await _context.Clubs
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Club>> GetActiveClubsAsync()
        {
            return await _context.Clubs
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Club>> GetPublicClubsAsync()
        {
            return await _context.Clubs
                .Where(c => c.IsPublic && c.IsActive && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Club> CreateAsync(Guid uid, Club club)
        {
            if (!await _context.UserRoles.AnyAsync(u => u.UserId == uid && u.RoleName.ToLower().Equals("admin")))
            {
                if (await _context.UserClubRoleAssignments.AnyAsync(ura => ura.ClubMember.UserId == uid && ura.ClubRole.Level == 0))
                {
                    throw new InvalidOperationException("A club manager already exists. Cannot create a new club without this manager.");
                }
            }
            club.CreatedAt = DateTime.UtcNow;
            club.IsDeleted = false;
            club.IsActive = true;
            await _context.Clubs.AddAsync(club);
            await _context.SaveChangesAsync();
            return club;
        }

        public async Task<bool> UpdateAsync(Club club)
        {
            try
            {
                club.UpdatedAt = DateTime.UtcNow;
                _context.Clubs.Update(club);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int clubId)
        {
            try
            {
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                    return false;

                _context.Clubs.Remove(club);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SoftDeleteAsync(int clubId)
        {
            try
            {
                var club = await GetByIdAsync(clubId);
                if (club == null)
                    return false;

                club.IsDeleted = true;
                club.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int clubId)
        {
            return await _context.Clubs
                .AnyAsync(c => c.ClubId == clubId && !c.IsDeleted);
        }

        public async Task<bool> ClubNameExistsAsync(string clubName)
        {
            return await _context.Clubs
                .AnyAsync(c => c.ClubName == clubName && !c.IsDeleted);
        }
        public async Task<int> CountMemberAsync(int clubId)
        {
            return await _context.UserClubRoles
                .CountAsync(m => m.ClubId == clubId && m.Status.ToUpper() == "ACTIVE");
        }
        public async Task ChangeStatusClub(int clubId)
        {
            var club = await GetByIdAsync(clubId);
            if (club != null)
            {
                club.IsActive = !club.IsActive;
                club.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> isDeleted(int clubId)
        {
            var club = await GetByIdAsync(clubId);
            if (club != null)
            {
                return !club.IsActive;
            }
            return false;

        }
    }
}
