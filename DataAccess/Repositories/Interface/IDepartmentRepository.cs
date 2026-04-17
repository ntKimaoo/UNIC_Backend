using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Repositories.Interface
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<Department>> GetAllAsync();
        Task<IEnumerable<Department>> GetByClubIdAsync(int clubId);
        Task<Department?> GetByIdAsync(int departmentId);
        Task<Department> CreateAsync(Department department);
        Task<bool> UpdateAsync(Department department);
        Task<bool> DeleteAsync(int departmentId);
        Task<Department?> GetByManagerRoleIdAsync(int managerRoleId);
        Task<bool> ExistsAsync(int departmentId);
        Task<bool> DepartmentNameExistsInClubAsync(string departmentName, int clubId, int? excludeDepartmentId = null);

        /// <summary>
        /// Returns all UserClubRole entries that belong to the given department
        /// (via UserClubRoleDepartment), including the user and club-role data.
        /// </summary>
        Task<IEnumerable<UserClubRole>> GetMembersWithRolesByDepartmentAsync(int clubId, int departmentId);
        Task<UserClubRoleDepartment> AddMemberTodepartment(UserClubRoleDepartment departMember);
        Task<UserClubRoleDepartment> RemoveMemberFromDepartment(UserClubRoleDepartment departMember);

        /// <summary>
        /// Returns all UserClubRole entries for members of the club who are NOT in the given department.
        /// </summary>
        Task<IEnumerable<UserClubRole>> GetClubMembersNotInDepartmentAsync(int clubId, int departmentId);

        /// <summary>
        /// Returns all departments (within the club) that the given club member HAS joined.
        /// </summary>
        Task<IEnumerable<Department>> GetDepartmentsJoinedByMemberAsync(int clubId, int clubMemberId);

        /// <summary>
        /// Returns all departments (within the club) that the given club member has NOT joined.
        /// </summary>
        Task<IEnumerable<Department>> GetDepartmentsNotJoinedByMemberAsync(int clubId, int clubMemberId);
    }
}
