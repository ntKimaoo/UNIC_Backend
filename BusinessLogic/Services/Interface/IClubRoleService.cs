using BusinessLogic.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

namespace BusinessLogic.Services.Interface
{
    public interface IClubRoleService
    {
        Task<ClubRoleResponseDto?> GetByIdAsync(int clubRoleId,int clubId);
        Task<IEnumerable<ClubRoleResponseDto>> GetAllAsync(int clubId);
        Task<ClubRoleResponseDto> CreateAsync(CreateClubRoleDto dto,int clubId);
        Task<ClubRoleResponseDto?> UpdateAsync(int clubRoleId, UpdateClubRoleDto dto,int clubId);
        Task<bool> DeleteAsync(int clubRoleId);
        Task UpdatePoliciesAsync(int clubRoleId, List<int> policyIds);
    }
}
