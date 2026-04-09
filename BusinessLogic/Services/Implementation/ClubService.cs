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

        public ClubService(IClubRepository repository)
        {
            _repository = repository;
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

        public async Task<ClubResponseDto> CreateAsync(CreateClubDto dto)
        {
            // Check if club name already exists
            if (await _repository.ClubNameExistsAsync(dto.ClubName))
            {
                throw new InvalidOperationException("Club name already exists");
            }

            var club = new Club
            {
                ClubName = dto.ClubName,
                ShortName = dto.ShortName,
                Description = dto.Description,
                FoundedDate = dto.FoundedDate,
                Status = dto.Status ?? "Active",
                IsPublic = dto.IsPublic,
                LogoUrl = dto.LogoUrl,
                CoverImageUrl = dto.CoverImageUrl,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FacebookUrl = dto.FacebookUrl,
                WebsiteUrl = dto.WebsiteUrl,
                Address = dto.Address,
                IsActive = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdClub = await _repository.CreateAsync(club);
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
            if(dto.IsActive.HasValue)
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
    }
}
