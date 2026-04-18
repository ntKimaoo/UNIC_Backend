using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;

namespace BusinessLogic.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        private UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                Avatar = user.Avatar,
                StudentId = user.StudentId,
                Major = user.Major,
                JoinDate = user.JoinDate,
                Status = user.Status,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<PagedResultDto<UserResponseDto>> GetPagedUsersAsync(int pageNumber, int pageSize)
        {
            var (items, totalCount) = await _userRepository.GetPagedAsync(pageNumber, pageSize);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResultDto<UserResponseDto>
            {
                Items = items.Select(MapToDto),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto request)
        {
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                throw new Exception("Email already exists.");
            }

            if (!string.IsNullOrEmpty(request.StudentId) && await _userRepository.StudentIdExistsAsync(request.StudentId))
            {
                throw new Exception("Student ID already exists.");
            }

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                StudentId = request.StudentId,
                Major = request.Major,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                Status = "Active",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(newUser);
            return MapToDto(createdUser);
        }

        public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            if (!string.IsNullOrEmpty(request.StudentId) &&
                request.StudentId != user.StudentId &&
                await _userRepository.StudentIdExistsAsync(request.StudentId))
            {
                throw new Exception("Student ID already exists.");
            }

            // Only update fields that are explicitly provided (non-null)
            if (request.FullName != null) user.FullName = request.FullName;
            if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
            if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
            if (request.Gender != null) user.Gender = request.Gender;
            if (request.Address != null) user.Address = request.Address;
            if (request.Avatar != null) user.Avatar = request.Avatar;
            if (request.Major != null) user.Major = request.Major;
            if (request.StudentId != null) user.StudentId = request.StudentId;

            if (!string.IsNullOrEmpty(request.Status))
            {
                user.Status = request.Status;
            }

            user.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            return await _userRepository.DeleteAsync(id);
        }
        public async Task<IEnumerable<Club>> GetAllClubsById(Guid UserId)
        {
            return await _userRepository.GetAllClubByUser(UserId);
        }

        public async Task<IEnumerable<UserDepartmentDto>?> GetUserDepartmentsInClubAsync(
            Guid userId, int clubId)
        {
            // Check membership first
            var clubs = await _userRepository.GetAllClubByUser(userId);
            if (!clubs.Any(c => c.ClubId == clubId))
                return null; // user is not a member of this club

            var rows = await _userRepository.GetDepartmentsByUserAndClubAsync(userId, clubId);

            return rows.Select(r => new UserDepartmentDto
            {
                DepartmentId = r.Department.DepartmentId,
                DepartmentName = r.Department.DepartmentName,
                Description = r.Department.Description,
                DepartmentRole = r.DepartmentRole == null ? null : new DepartmentMemberRoleDto
                {
                    ClubRoleId = r.DepartmentRole.ClubRoleId,
                    RoleName = r.DepartmentRole.RoleName,
                    Description = r.DepartmentRole.Description,
                    Level = r.DepartmentRole.Level
                },
                Roles = (r.Department.ClubRoles ?? Enumerable.Empty<ClubRole>())
                    .Select(cr => new DepartmentMemberRoleDto
                    {
                        ClubRoleId = cr.ClubRoleId,
                        RoleName = cr.RoleName,
                        Description = cr.Description,
                        Level = cr.Level
                    })
                    .ToList(),
                MemberCount = r.MemberCount
            });
        }

        public Task<bool> AssignUserRole(Guid userId, string roleName)
        {
            return _userRepository.AssignUserRole(userId, roleName);
        }

        public Task<string> GetUserRole(Guid userId)
        {
            return _userRepository.GetUserRole(userId);
        }
        public async Task<bool> isActiveAccount(Guid userId)
        {
            return await _userRepository.isActiveAccount(userId);
        }

        public async Task<UserResponseDto> GetByEmail(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<UserResponseDto>> Search(string query)
        {
            return await _userRepository.Search(query).ContinueWith(t => t.Result.Select(MapToDto));
        }
    }
}