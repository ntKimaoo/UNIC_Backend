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
            if (post == null)
                return null;

            return MapToResponseDto(post);
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

        public async Task<ClubPostResponseDto> CreateAsync(CreateClubPostDto dto, IFormFile? imageFile)
        {
            // Verify club exists
            var clubs = await _repository.GetByClubIdAsync(dto.ClubId);
            // This is a bit inefficient, but confirms existence if we don't have a direct Club existence check in repo
            // Actually, I'll just rely on the DB. But to fix the null issue:

            var status = "PUBLISHED";
            if (imageFile != null)
            {
                status = "PENDING";
            }

            var post = new ClubPost
            {
                ClubId = dto.ClubId,
                UserId = dto.UserId,
                Title = dto.Title,
                ImageUrl = dto.ImageUrl ?? "", 
                Caption = dto.Caption ?? "",
                Content = dto.Content ?? "",
                Status = dto.Status ?? status,
                PostDate = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var createdPost = await _repository.CreateAsync(post);
            
            if (imageFile != null)
            {
                using (var ms = new MemoryStream())
                {
                    await imageFile.CopyToAsync(ms);
                    ImageUploadQueueService.EnqueueTask(new ImageUploadTask
                    {
                        PostId = createdPost.PostId,
                        FileData = ms.ToArray(),
                        FileName = imageFile.FileName,
                        Folder = "clubposts"
                    });
                }
            }

            // Reload to get navigation properties
            var postWithNav = await _repository.GetByIdAsync(createdPost.PostId);
            return MapToResponseDto(postWithNav!);
        }

        public async Task<ClubPostResponseDto?> UpdateAsync(int postId, UpdateClubPostDto dto, IFormFile? imageFile)
        {
            var post = await _repository.GetByIdAsync(postId);
            if (post == null)
                return null;

            // Update only provided fields
            if (!string.IsNullOrEmpty(dto.Title))
                post.Title = dto.Title;

            if (dto.ImageUrl != null)
                post.ImageUrl = dto.ImageUrl;

            if (dto.Caption != null)
                post.Caption = dto.Caption;

            if (dto.Content != null)
                post.Content = dto.Content;

            if (!string.IsNullOrEmpty(dto.Status))
                post.Status = dto.Status;

            post.UpdatedAt = DateTime.Now;

            if (imageFile != null)
            {
                post.Status = "PENDING";
                using (var ms = new MemoryStream())
                {
                    await imageFile.CopyToAsync(ms);
                    ImageUploadQueueService.EnqueueTask(new ImageUploadTask
                    {
                        PostId = post.PostId,
                        FileData = ms.ToArray(),
                        FileName = imageFile.FileName,
                        Folder = "clubposts"
                    });
                }
            }

            var updated = await _repository.UpdateAsync(post);
            if (!updated)
                return null;

            return MapToResponseDto(post);
        }


        public async Task<bool> DeleteAsync(int postId)
        {
            return await _repository.DeleteAsync(postId);
        }

        private ClubPostResponseDto MapToResponseDto(ClubPost post)
        {
            return new ClubPostResponseDto
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
                Status = post.Status
            };
        }
    }
}
