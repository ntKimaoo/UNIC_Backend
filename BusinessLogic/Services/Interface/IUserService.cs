using BusinessLogic.DTOs;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}