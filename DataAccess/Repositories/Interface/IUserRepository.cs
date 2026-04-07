using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid userId);
        Task<User?> GetByStudentIdAsync(string studentId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> StudentIdExistsAsync(string studentId);
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid userId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Club>> GetAllClubByUser(Guid userId);

        /// <summary>
        /// Returns all departments a user belongs to within a specific club,
        /// along with the role they hold in each department (if any).
        /// </summary>
        Task<IEnumerable<(Department Department, ClubRole? DepartmentRole, int MemberCount)>> GetDepartmentsByUserAndClubAsync(Guid userId, int clubId);
    }
}
