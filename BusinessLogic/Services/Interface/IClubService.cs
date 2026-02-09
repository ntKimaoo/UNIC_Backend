using BusinessLogic.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IClubService
    {
        Task<ClubResponseDto?> GetByIdAsync(int clubId);
        Task<IEnumerable<ClubResponseDto>> GetAllAsync();
        Task<IEnumerable<ClubResponseDto>> GetActiveClubsAsync();
        Task<IEnumerable<ClubResponseDto>> GetPublicClubsAsync();
        Task<ClubResponseDto> CreateAsync(CreateClubDto dto);
        Task<ClubResponseDto?> UpdateAsync(int clubId, UpdateClubDto dto);
        Task<bool> DeleteAsync(int clubId);
        Task<bool> SoftDeleteAsync(int clubId);
    }
}
