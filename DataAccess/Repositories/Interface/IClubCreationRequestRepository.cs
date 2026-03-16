using UNIC.DataAccess.Models;

namespace DataAccess.Repositories.Interface
{
    public interface IClubCreationRequestRepository
    {
        Task<IEnumerable<ClubCreationRequest>> GetAllAsync();

        Task<ClubCreationRequest?> GetByIdAsync(int id);

        Task<IEnumerable<ClubCreationRequest>> GetByUserIdAsync(Guid userId);

        Task<bool> HasPendingRequestAsync(Guid userId);

        Task AddAsync(ClubCreationRequest request);

        Task UpdateAsync(ClubCreationRequest request);

        Task DeleteAsync(int id);
    }
}