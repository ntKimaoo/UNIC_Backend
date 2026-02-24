using BusinessLogic.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IClubRoleService
    {
        Task<ClubRoleResponseDto?> GetByIdAsync(int clubRoleId);
        Task<IEnumerable<ClubRoleResponseDto>> GetAllAsync();
        Task<ClubRoleResponseDto> CreateAsync(CreateClubRoleDto dto);
        Task<ClubRoleResponseDto?> UpdateAsync(int clubRoleId, UpdateClubRoleDto dto);
        Task<bool> DeleteAsync(int clubRoleId);
    }
}
