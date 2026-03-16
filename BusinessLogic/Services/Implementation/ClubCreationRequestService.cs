using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using UNIC.BusinessLogic.DTOs;
using UNIC.DataAccess.Models;

namespace BusinessLogic.Services
{
    public class ClubCreationRequestService : IClubCreationRequestService
    {
        private readonly IClubCreationRequestRepository _repository;

        public ClubCreationRequestService(IClubCreationRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ClubCreationRequestDto>> GetAllAsync()
        {
            var requests = await _repository.GetAllAsync();

            return requests.Select(r => new ClubCreationRequestDto
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                ClubName = r.ClubName,
                Description = r.Description,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            });
        }

        public async Task<ClubCreationRequestDto?> GetByIdAsync(int id)
        {
            var request = await _repository.GetByIdAsync(id);

            if (request == null) return null;

            return new ClubCreationRequestDto
            {
                RequestId = request.RequestId,
                UserId = request.UserId,
                ClubName = request.ClubName,
                Description = request.Description,
                Reason = request.Reason,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };
        }

        public async Task<IEnumerable<ClubCreationRequestDto>> GetByUserIdAsync(Guid userId)
        {
            var requests = await _repository.GetByUserIdAsync(userId);

            return requests.Select(r => new ClubCreationRequestDto
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                ClubName = r.ClubName,
                Description = r.Description,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            });
        }

        public async Task<bool> HasPendingRequestAsync(Guid userId)
        {
            return await _repository.HasPendingRequestAsync(userId);
        }

        public async Task<bool> CreateAsync(CreateClubCreationRequestDto dto)
        {
            var request = new ClubCreationRequest
            {
                UserId = dto.UserId,
                ClubName = dto.ClubName,
                Description = dto.Description,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(request);
            return true;
        }

        public async Task<bool> UpdateAsync(int id, UpdateClubCreationRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(id);

            if (request == null) return false;

            request.ClubName = dto.ClubName;
            request.Description = dto.Description;
            request.Reason = dto.Reason;
            request.Status = dto.Status;

            await _repository.UpdateAsync(request);

            return true;
        }
        public async Task<bool> UpdateStatusAsync(int id, UpdateClubRequestStatusDto dto)
        {
            var request = await _repository.GetByIdAsync(id);

            if (request == null)
                return false;

            request.Status = dto.Status;
            request.AdminComment = dto.AdminComment;
            request.ReviewedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(request);

            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            return true;
        }
    }
}