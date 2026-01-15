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
    public class MemberRepository : IMemberRepository
    {
        private readonly UnicAuthenticateContext _context;

        public MemberRepository(UnicAuthenticateContext context)
        {
            _context = context;
        }

        public async Task<Member?> GetByEmailAsync(string email)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.Email == email);
        }

        public async Task<Member?> GetByIdAsync(int memberId)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);
        }

        public async Task<Member?> GetByStudentIdAsync(string studentId)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.StudentId == studentId);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Members
                .AnyAsync(m => m.Email == email);
        }

        public async Task<bool> StudentIdExistsAsync(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
                return false;

            return await _context.Members
                .AnyAsync(m => m.StudentId == studentId);
        }

        public async Task<Member> CreateAsync(Member member)
        {
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task<bool> UpdateAsync(Member member)
        {
            try
            {
                member.UpdatedAt = DateTime.UtcNow;
                _context.Members.Update(member);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int memberId)
        {
            try
            {
                var member = await GetByIdAsync(memberId);
                if (member == null)
                    return false;

                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
