using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class UserRepository : IUserRepository
    {
        private readonly UnicContext _context;

        public UserRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(m => m.Email == email);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<User?> GetByStudentIdAsync(string studentId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(m => m.StudentId == studentId);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(m => m.Email == email);
        }

        public async Task<bool> StudentIdExistsAsync(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
                return false;

            return await _context.Users
                .AnyAsync(m => m.StudentId == studentId);
        }

        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleName = "User"
            });
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Guid userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }
        public async Task<IEnumerable<Club>> GetAllClubByUser(Guid userId)
        {
            return await _context.UserClubRoles
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.Club)
                .ToListAsync();
        }
    }
}
