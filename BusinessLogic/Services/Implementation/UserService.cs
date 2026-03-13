using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth;
            user.Gender = request.Gender;
            user.Address = request.Address;
            user.Avatar = request.Avatar;
            user.Major = request.Major;
            user.StudentId = request.StudentId;

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
    }
}