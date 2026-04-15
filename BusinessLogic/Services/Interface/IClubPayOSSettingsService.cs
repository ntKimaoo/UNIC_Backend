using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IClubPayOSSettingsService
    {
        Task<ClubPayOSSettingsResponseDto> GetAsync(Guid currentUserId, int clubId, bool isSystemAdmin);
        Task<ClubPayOSSettingsResponseDto> UpsertAsync(Guid currentUserId, int clubId, bool isSystemAdmin, UpsertClubPayOSSettingsDto dto);
    }
}

