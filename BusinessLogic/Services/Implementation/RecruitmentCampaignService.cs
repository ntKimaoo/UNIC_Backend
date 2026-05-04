using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class RecruitmentCampaignService : IRecruitmentCampaignService
    {
        private readonly IRecruitmentCampaignRepository _repository;
        private readonly IClubRepository _clubRepository;

        public RecruitmentCampaignService(
            IRecruitmentCampaignRepository repository,
            IClubRepository clubRepository)
        {
            _repository = repository;
            _clubRepository = clubRepository;
        }

        public async Task<RecruitmentCampaignResponseDto?> GetByIdAsync(int campaignId)
        {
            var campaign = await _repository.GetByIdAsync(campaignId);
            if (campaign == null)
                return null;

            return MapToResponseDto(campaign);
        }

        public async Task<IEnumerable<RecruitmentCampaignResponseDto>> GetAllAsync()
        {
            var campaigns = await _repository.GetAllAsync();
            return campaigns.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<RecruitmentCampaignResponseDto>> GetByClubIdAsync(int clubId)
        {
            var campaigns = await _repository.GetByClubIdAsync(clubId);
            return campaigns.Select(MapToResponseDto);
        }

        public async Task<PagedResultDto<RecruitmentCampaignResponseDto>> GetPagedAsync(
            int page, int pageSize, string? search, string? filterBy, bool ascending)
        {
            var (items, total) = await _repository.GetPagedAsync(page, pageSize, search, filterBy, ascending);
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return new PagedResultDto<RecruitmentCampaignResponseDto>
            {
                Items = items.Select(MapToResponseDto),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public async Task<PagedResultDto<RecruitmentCampaignResponseDto>> GetPagedByClubIdAsync(
            int clubId, int page, int pageSize, string? search, string? filterBy, bool ascending)
        {
            var (items, total) = await _repository.GetPagedByClubIdAsync(
                clubId, page, pageSize, search, filterBy, ascending);

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return new PagedResultDto<RecruitmentCampaignResponseDto>
            {
                Items = items.Select(MapToResponseDto),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public async Task<RecruitmentCampaignResponseDto> CreateAsync(CreateRecruitmentCampaignDto dto)
        {
            var clubExists = await _clubRepository.ExistsAsync(dto.ClubId);
            if (!clubExists)
                throw new NotFoundException("Club", dto.ClubId);

            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.StartDate >= dto.EndDate)
                throw new DomainException("StartDate must be earlier than EndDate.");

            if (dto.StartDate.HasValue && dto.EndDate.HasValue)
            {
                var hasOverlap = await _repository.HasOverlappingCampaignAsync(
                    dto.ClubId, dto.StartDate.Value, dto.EndDate.Value);
                if (hasOverlap)
                    throw new DomainException("Câu lạc bộ đã có đợt tuyển dụng trong khoảng thời gian này.");
            }

            var validStatuses = new[] { "OPEN", "CLOSED", "DRAFT" };
            if (!string.IsNullOrEmpty(dto.Status) && !validStatuses.Contains(dto.Status.ToUpper()))
                throw new DomainException($"Invalid status. Allowed values: {string.Join(", ", validStatuses)}");

            var campaign = new RecruitmentCampaign
            {
                ClubId = dto.ClubId,
                CampaignName = dto.CampaignName,
                LinkCampaign = dto.LinkCampaign,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status ?? "OPEN",
                ImageUrl = dto.ImageUrl,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            var createdCampaign = await _repository.CreateAsync(campaign);
            return MapToResponseDto(createdCampaign);
        }

        public async Task<RecruitmentCampaignResponseDto?> UpdateAsync(int campaignId, UpdateRecruitmentCampaignDto dto)
        {
            var campaign = await _repository.GetByIdAsync(campaignId);
            if (campaign == null)
                return null;

            var validStatuses = new[] { "OPEN", "CLOSED", "DRAFT" };
            if (!string.IsNullOrEmpty(dto.Status) && !validStatuses.Contains(dto.Status.ToUpper()))
                throw new DomainException($"Invalid status. Allowed values: {string.Join(", ", validStatuses)}");

            var effectiveStartDate = dto.StartDate ?? campaign.StartDate;
            var effectiveEndDate = dto.EndDate ?? campaign.EndDate;
            if (effectiveStartDate.HasValue && effectiveEndDate.HasValue && effectiveStartDate >= effectiveEndDate)
                throw new DomainException("StartDate must be earlier than EndDate.");

            // Update only provided fields
            if (!string.IsNullOrEmpty(dto.CampaignName))
                campaign.CampaignName = dto.CampaignName;

            if (dto.LinkCampaign != null)
                campaign.LinkCampaign = dto.LinkCampaign;

            if (dto.Description != null)
                campaign.Description = dto.Description;

            if (dto.StartDate.HasValue)
                campaign.StartDate = dto.StartDate;

            if (dto.EndDate.HasValue)
                campaign.EndDate = dto.EndDate;

            if (!string.IsNullOrEmpty(dto.Status))
                campaign.Status = dto.Status.ToUpper();

            if (dto.ImageUrl != null)
                campaign.ImageUrl = dto.ImageUrl;

            if (dto.Content != null)
                campaign.Content = dto.Content;

            var updated = await _repository.UpdateAsync(campaign);
            if (!updated)
                return null;

            return MapToResponseDto(campaign);
        }

        public async Task<bool> DeleteAsync(int campaignId)
        {
            return await _repository.DeleteAsync(campaignId);
        }

        private RecruitmentCampaignResponseDto MapToResponseDto(RecruitmentCampaign campaign)
        {
            return new RecruitmentCampaignResponseDto
            {
                CampaignId = campaign.CampaignId,
                ClubId = campaign.ClubId,
                CampaignName = campaign.CampaignName,
                LinkCampaign = campaign.LinkCampaign,
                Description = campaign.Description,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                Status = campaign.Status,
                ImageUrl = campaign.ImageUrl,
                Content = campaign.Content,
                CreatedAt = campaign.CreatedAt
            };
        }
    }
}
