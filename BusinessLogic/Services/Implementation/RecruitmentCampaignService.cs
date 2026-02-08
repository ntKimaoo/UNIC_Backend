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
    public class RecruitmentCampaignService : IRecruitmentCampaignService
    {
        private readonly IRecruitmentCampaignRepository _repository;

        public RecruitmentCampaignService(IRecruitmentCampaignRepository repository)
        {
            _repository = repository;
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

        public async Task<RecruitmentCampaignResponseDto> CreateAsync(CreateRecruitmentCampaignDto dto)
        {
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
                campaign.Status = dto.Status;

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
