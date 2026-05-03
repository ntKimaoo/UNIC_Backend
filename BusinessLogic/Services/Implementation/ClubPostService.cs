using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class ClubPostService : IClubPostService
    {
        private readonly IClubPostRepository _repository;

        public ClubPostService(IClubPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClubPostResponseDto?> GetByIdAsync(int postId)
        {
            var post = await _repository.GetByIdAsync(postId);
            return post == null ? null : MapToResponseDto(post);
        }

        public async Task<IEnumerable<ClubPostResponseDto>> GetAllAsync()
        {
            var posts = await _repository.GetAllAsync();
            return posts.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubPostResponseDto>> GetByClubIdAsync(int clubId)
        {
            var posts = await _repository.GetByClubIdAsync(clubId);
            return posts.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubPostResponseDto>> GetByUserIdAsync(Guid userId)
        {
            var posts = await _repository.GetByUserIdAsync(userId);
            return posts.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubPostResponseDto>> GetByEventIdAsync(int eventId)
        {
            var posts = await _repository.GetByEventIdAsync(eventId);
            return posts.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ClubPostResponseDto>> GetByCampaignIdAsync(int campaignId)
        {
            var posts = await _repository.GetByCampaignIdAsync(campaignId);
            return posts.Select(MapToResponseDto);
        }

        public async Task<ClubPostResponseDto> CreateAsync(CreateClubPostDto dto, IFormFile? imageFile)
        {
            var status = imageFile != null ? "PENDING" : "PUBLISHED";

            var post = new ClubPost
            {
                ClubId = dto.ClubId,
                UserId = dto.UserId,
                Title = dto.Title,
                ImageUrl = dto.ImageUrl ?? "",
                Caption = dto.Caption ?? "",
                Content = dto.Content ?? "",
                Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : status,
                EventId = dto.EventId,
                CampaignId = dto.CampaignId,
                PostDate = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var created = await _repository.CreateAsync(post);

            if (imageFile != null)
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                ImageUploadQueueService.EnqueueTask(new ImageUploadTask
                {
                    PostId = created.PostId,
                    FileData = ms.ToArray(),
                    FileName = imageFile.FileName,
                    Folder = "clubposts"
                });
            }

            var postWithNav = await _repository.GetByIdAsync(created.PostId);
            return MapToResponseDto(postWithNav!);
        }

        public async Task<ClubPostResponseDto?> UpdateAsync(int postId, UpdateClubPostDto dto, IFormFile? imageFile)
        {
            var post = await _repository.GetByIdAsync(postId);
            if (post == null) return null;

            if (!string.IsNullOrEmpty(dto.Title))     post.Title = dto.Title;
            if (dto.ImageUrl != null)                  post.ImageUrl = dto.ImageUrl;
            if (dto.Caption != null)                   post.Caption = dto.Caption;
            if (dto.Content != null)                   post.Content = dto.Content;
            if (!string.IsNullOrEmpty(dto.Status))     post.Status = dto.Status;
            if (dto.EventId.HasValue)                  post.EventId = dto.EventId;
            if (dto.CampaignId.HasValue)               post.CampaignId = dto.CampaignId;

            post.UpdatedAt = DateTime.Now;

            if (imageFile != null)
            {
                post.Status = "PENDING";
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                ImageUploadQueueService.EnqueueTask(new ImageUploadTask
                {
                    PostId = post.PostId,
                    FileData = ms.ToArray(),
                    FileName = imageFile.FileName,
                    Folder = "clubposts"
                });
            }

            var updated = await _repository.UpdateAsync(post);
            return updated ? MapToResponseDto(post) : null;
        }

        public async Task<bool> DeleteAsync(int postId)
        {
            return await _repository.DeleteAsync(postId);
        }

        private static ClubPostResponseDto MapToResponseDto(ClubPost post) => new()
        {
            PostId = post.PostId,
            ClubId = post.ClubId,
            ClubName = post.Club?.ClubName ?? "",
            UserId = post.UserId,
            UserName = post.User?.FullName,
            Title = post.Title,
            ImageUrl = post.ImageUrl,
            Caption = post.Caption,
            Content = post.Content,
            PostDate = post.PostDate,
            UpdatedAt = post.UpdatedAt,
            Status = post.Status,
            IsDeleted = post.IsDeleted,
            EventId = post.EventId,
            EventName = post.Event?.EventName,
            CampaignId = post.CampaignId,
            CampaignName = post.RecruitmentCampaign?.CampaignName
        };
    }
}
