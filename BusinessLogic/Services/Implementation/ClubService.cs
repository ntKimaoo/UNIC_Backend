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
    public class ClubService : IClubService
    {
        private readonly IClubRepository _repository;
        private readonly IClubRoleService _clubRoleService;
        private readonly IClubMemberService _memberService;

        public ClubService(IClubRepository repository, IClubRoleService clubRoleService, IClubMemberService memberService)
        {
            _repository = repository;
            _clubRoleService = clubRoleService;
            _memberService = memberService;
        }

        public async Task<ClubResponseDto?> GetByIdAsync(int clubId)
        {
            var club = await _repository.GetByIdAsync(clubId);
            if (club == null)
                return null;

            return MapToResponseDto(club);
        }

        public async Task<IEnumerable<ClubResponseDto>> GetAllAsync()
        {
            var clubs = await _repository.GetAllAsync();
            return clubs.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubResponseDto>> GetActiveClubsAsync()
        {
            var clubs = await _repository.GetActiveClubsAsync();
            return clubs.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubResponseDto>> GetPublicClubsAsync()
        {
            var clubs = await _repository.GetPublicClubsAsync();
            return clubs.Select(MapToResponseDto);
        }

        public async Task<ClubResponseDto> CreateAsync(Guid uid, CreateClubDto dto)
        {
            // Check if club name already exists
            if (await _repository.ClubNameExistsAsync(dto.ClubName))
            {
                throw new InvalidOperationException("Club name already exists");
            }

            var club = new Club
            {
                ClubName = dto.ClubName,
                ShortName = dto.ShortName ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                FoundedDate = dto.FoundedDate,
                Status = dto.Status ?? "Active",
                IsPublic = dto.IsPublic,
                LogoUrl = dto.LogoUrl ?? string.Empty,
                CoverImageUrl = dto.CoverImageUrl ?? string.Empty,
                Email = dto.Email ?? string.Empty,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                FacebookUrl = dto.FacebookUrl ?? string.Empty,
                WebsiteUrl = dto.WebsiteUrl ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                IsActive = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdClub = await _repository.CreateAsync(uid, club);

            var managerRole = await _clubRoleService.CreateAsync(new CreateClubRoleDto
            {
                RoleName = "Club Manager",
                Description = "Vai trò chủ nhiệm câu lạc bộ, có toàn quyền quản lý và điều hành các hoạt động của câu lạc bộ.",
                Level = 0,
                policies = new List<int>()
            }, createdClub.ClubId);

            await _memberService.AddUserToClubAsync(createdClub.ClubId, new AddUserToClubDto
            {
                UserId = uid,
                ClubRoleIds = new List<int> { managerRole.ClubRoleId }
            }, uid);

            return MapToResponseDto(createdClub);
        }

        public async Task<ClubResponseDto?> UpdateAsync(int clubId, UpdateClubDto dto)
        {
            var club = await _repository.GetByIdAsync(clubId);
            if (club == null)
                return null;


            var nameExists = await _repository.ClubNameExistsAsync(dto.ClubName);
            if (nameExists && club.ClubName != dto.ClubName)
            {
                throw new InvalidOperationException("Club name already exists");
            }
            club.ClubName = dto.ClubName;

            club.ShortName = dto.ShortName;

            club.Description = dto.Description;

            club.FoundedDate = dto.FoundedDate;

            club.Status = dto.Status;
            if (dto.IsPublic.HasValue)
                club.IsPublic = dto.IsPublic.Value;

            club.LogoUrl = dto.LogoUrl;

            club.CoverImageUrl = dto.CoverImageUrl;

            club.Email = dto.Email;

            club.PhoneNumber = dto.PhoneNumber;

            club.FacebookUrl = dto.FacebookUrl;

            club.WebsiteUrl = dto.WebsiteUrl;

            club.Address = dto.Address;
            if (dto.IsActive.HasValue)
                club.IsActive = dto.IsActive.Value;
            
                club.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(club);
            if (!updated)
                return null;

            return MapToResponseDto(club);
        }

        public async Task<bool> DeleteAsync(int clubId)
        {
            return await _repository.DeleteAsync(clubId);
        }

        public async Task<bool> SoftDeleteAsync(int clubId)
        {
            return await _repository.SoftDeleteAsync(clubId);
        }
        public async Task ChangeStatusClub(int clubId)
        {
            await _repository.ChangeStatusClub(clubId);
        }
        private ClubResponseDto MapToResponseDto(Club club)
        {
            return new ClubResponseDto
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                ShortName = club.ShortName,
                Description = club.Description,
                FoundedDate = club.FoundedDate,
                Status = club.Status,
                IsPublic = club.IsPublic,
                LogoUrl = club.LogoUrl,
                CoverImageUrl = club.CoverImageUrl,
                Email = club.Email,
                PhoneNumber = club.PhoneNumber,
                FacebookUrl = club.FacebookUrl,
                WebsiteUrl = club.WebsiteUrl,
                Address = club.Address,
                CreatedAt = club.CreatedAt,
                UpdatedAt = club.UpdatedAt,
                IsActive = club.IsActive,
                IsDeleted = club.IsDeleted
            };
        }
        public async Task<bool> isDeleted(int clubId)
        {
            return await _repository.isDeleted(clubId);
        }
    }
}