using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IMemberRepository
    {
        Task<Member?> GetByEmailAsync(string email);
        Task<Member?> GetByIdAsync(Guid memberId);
        Task<Member?> GetByStudentIdAsync(string studentId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> StudentIdExistsAsync(string studentId);
        Task<Member> CreateAsync(Member member);
        Task<bool> UpdateAsync(Member member);
        Task<bool> DeleteAsync(Guid memberId);
    }
}
