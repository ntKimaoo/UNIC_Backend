using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IClubRepository
    {
        Task<Club?> GetByIdAsync(int clubId);
        Task<IEnumerable<Club>> GetAllAsync();
        Task<IEnumerable<Club>> GetActiveClubsAsync();
        Task<IEnumerable<Club>> GetPublicClubsAsync();
        Task<Club> CreateAsync(Club club);
        Task<bool> UpdateAsync(Club club);
        Task<bool> DeleteAsync(int clubId);
        Task<bool> SoftDeleteAsync(int clubId);
        Task<bool> ExistsAsync(int clubId);
        Task<bool> ClubNameExistsAsync(string clubName);
        Task<int> CountMemberAsync(int clubId);
        Task ChangeStatusClub(int clubId);
    }
}
