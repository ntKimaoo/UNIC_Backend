using BusinessLogic.DTOs;
using UNIC.BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IClubCreationRequestService
    {
        Task<IEnumerable<ClubCreationRequestDto>> GetAllAsync();

        Task<ClubCreationRequestDto?> GetByIdAsync(int id);

        Task<IEnumerable<ClubCreationRequestDto>> GetByUserIdAsync(Guid userId);

        Task<bool> HasPendingRequestAsync(Guid userId);

        Task<bool> CreateAsync(CreateClubCreationRequestDto dto);

        Task<bool> UpdateAsync(int id, UpdateClubCreationRequestDto dto);
        Task<ClubCreationRequestDto?> UpdateStatusAsync(int id, UpdateClubRequestStatusDto dto);
        Task<bool> DeleteAsync(int id);
    }
}