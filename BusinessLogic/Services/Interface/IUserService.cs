using BusinessLogic.DTOs;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<PagedResultDto<UserResponseDto>> GetPagedUsersAsync(int pageNumber, int pageSize);
        Task<UserResponseDto?> GetUserByIdAsync(Guid id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto request);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto request);
        Task<bool> DeleteUserAsync(Guid id);
        Task<IEnumerable<Club>> GetAllClubsById(Guid UserId);

        /// <summary>
        /// Returns all departments the user has joined within a club,
        /// along with their department-specific role (null if none).
        /// Returns null when the user is not a member of the club.
        /// </summary>
        Task<IEnumerable<UserDepartmentDto>?> GetUserDepartmentsInClubAsync(Guid userId, int clubId);
        Task<bool> isActiveAccount(Guid userId);
    }
}