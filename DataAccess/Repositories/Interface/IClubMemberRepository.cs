using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IClubMemberRepository
    {
        /// <summary>
        /// Lấy tất cả members của một club
        /// </summary>
        Task<IEnumerable<UserClubRole>> GetMembersByClubIdAsync(int clubId);

        /// <summary>
        /// Lấy thông tin member theo ID
        /// </summary>
        Task<UserClubRole?> GetMemberByIdAsync(int clubMemberId);

        /// <summary>
        /// Tìm member theo userId và clubId (để kiểm tra đã tồn tại chưa)
        /// </summary>
        Task<UserClubRole?> GetMemberAsync(Guid userId, int clubId);

        /// <summary>
        /// Thêm user vào club
        /// </summary>
        Task<UserClubRole> AddMemberAsync(UserClubRole member);

        /// <summary>
        /// Cập nhật thông tin member (role, status)
        /// </summary>
        Task<bool> UpdateMemberAsync(UserClubRole member);

        /// <summary>
        /// Xóa member khỏi club
        /// </summary>
        Task<bool> RemoveMemberAsync(int clubMemberId);

        /// <summary>
        /// Kiểm tra user đã là member của club chưa
        /// </summary>
        Task<bool> IsMemberAsync(Guid userId, int clubId);

        /// <summary>
        /// Lấy tất cả clubs mà user đã gia nhập kèm role
        /// </summary>
        Task<IEnumerable<UserClubRole>> GetClubsByUserIdAsync(Guid userId);
    }
}
