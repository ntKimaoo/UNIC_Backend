using DataAccess.Models;

namespace DataAccess.Repositories.Interface
{
    public interface IClubPayOSSettingsRepository
    {
        Task<ClubPayOSSettings?> GetByClubIdAsync(int clubId);
        Task UpsertAsync(ClubPayOSSettings settings);
    }
}

