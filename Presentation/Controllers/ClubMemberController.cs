using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UNIC.BusinessLogic.Services.Interface;

namespace Presentation.Controllers
{
    [ApiController]

    public class ClubMemberController : ControllerBase
    {
        private readonly IClubMemberService _service;
        private readonly IPolicyService _policyService;
        private readonly IDepartmentService _departmentService;

        public ClubMemberController(IClubMemberService service, IPolicyService policyService, IDepartmentService departmentService)
        {
            _service = service;
            _policyService = policyService;
            _departmentService = departmentService;
        }
        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? User.FindFirst("userId");

            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy danh sách members của club (hỗ trợ phân trang, lọc, sắp xếp)
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members")]
        
        public async Task<IActionResult> GetMembers(
            int clubId,
            [FromQuery] int? pagination,
            [FromQuery] int? page,
            [FromQuery] string? filter,
            [FromQuery] bool? ascending,
            [FromQuery] string? sortBy)
        {
            var pagedResult = await _service.GetMembersByClubAsync(clubId, pagination, page, filter, ascending, sortBy);

            if (pagination.HasValue && pagination.Value > 0)
            {
                return Ok(new
                {
                    success = true,
                    totalItems = pagedResult.TotalCount,
                    totalPages = pagedResult.TotalPages,
                    currentPage = pagedResult.PageNumber,
                    pageSize = pagedResult.PageSize,
                    data = pagedResult.Items
                });
            }

            return Ok(new { success = true, totalItems = pagedResult.TotalCount, data = pagedResult.Items });
        }

        /// <summary>
        /// Lấy thông tin một member theo ID
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members/{memberId}")]
       
        public async Task<IActionResult> GetMember(int clubId, int memberId)
        {
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            return Ok(new { success = true, data = member });
        }

        /// <summary>
        /// Thêm user vào club
        /// </summary>
        [HttpPost("api/clubs/{clubId}/members")]
        
        public async Task<IActionResult> AddMember(int clubId, [FromBody] AddUserToClubDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var assignedBy = GetCurrentUserId();
                var member = await _service.AddUserToClubAsync(clubId, dto, assignedBy);

                return CreatedAtAction(nameof(GetMember), new { clubId, memberId = member.ClubMemberId }, new
                {
                    success = true,
                    message = "User added to club successfully",
                    data = member
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật role của member trong club
        /// </summary>
        [HttpPut("api/clubs/{clubId}/members/{memberId}/role")]
        
        public async Task<IActionResult> UpdateMemberRole(int clubId, int memberId, [FromBody] UpdateMemberRoleDto? dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var member = await _service.UpdateMemberRoleAsync(memberId, dto);
                if (member == null || member.ClubId != clubId)
                    return NotFound(new { success = false, message = "Member not found" });

                return Ok(new { success = true, message = "Member role updated successfully", data = member });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, error = ex.Message });
            }
        }

        /// <summary>
        /// Active/deactive member trong club
        /// </summary>
        [HttpPut("api/clubs/{clubId}/members/{memberId}/status")]
        public async Task<IActionResult> UpdateMemberStatus(int clubId, int memberId, [FromBody] UpdateMemberStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var member = await _service.GetMemberByIdAsync(memberId);
                if (member == null || member.ClubId != clubId)
                    return NotFound(new { success = false, message = "Member not found" });

                var updated = await _service.UpdateMemberStatusAsync(memberId, dto.IsActive);
                if (updated == null)
                    return StatusCode(500, new { success = false, message = "Failed to update member status" });

                return Ok(new
                {
                    success = true,
                    message = dto.IsActive ? "Member activated successfully" : "Member deactivated successfully",
                    data = updated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa member khỏi club
        /// </summary>
        [HttpDelete("api/clubs/{clubId}/members/{memberId}")]
        
        public async Task<IActionResult> RemoveMember(int clubId, int memberId)
        {
            // Kiểm tra member có thuộc club này không
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            var result = await _service.RemoveMemberAsync(memberId);
            if (!result)
                return StatusCode(500, new { success = false, message = "Failed to remove member" });

            return Ok(new { success = true, message = "Member removed from club successfully" });
        }

        // ─── Role Extension Endpoints ─────────────────────────────────────

        /// <summary>
        /// Gán thêm 1 role cho member
        /// </summary>
        [HttpPost("api/clubs/{clubId}/members/{memberId}/roles/{roleId}")]
        public async Task<IActionResult> AddMemberRole(int clubId, int memberId, int roleId)
        {
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            var roles = member.Roles.Select(r => r.ClubRoleId).ToList();
            if (!roles.Contains(roleId))
            {
                roles.Add(roleId);
                await _service.UpdateMemberRoleAsync(memberId, new UpdateMemberRoleDto { ClubRoleIds = roles });
            }
            return Ok(new { success = true, message = "Role added successfully" });
        }

        /// <summary>
        /// Xóa 1 role khỏi member
        /// </summary>
        [HttpDelete("api/clubs/{clubId}/members/{memberId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveMemberRole(int clubId, int memberId, int roleId)
        {
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            var roles = member.Roles.Select(r => r.ClubRoleId).ToList();
            if (roles.Contains(roleId))
            {
                roles.Remove(roleId);
                await _service.UpdateMemberRoleAsync(memberId, new UpdateMemberRoleDto { ClubRoleIds = roles });
            }
            return Ok(new { success = true, message = "Role removed successfully" });
        }

        /// <summary>
        /// Lấy danh sách club mà user đã gia nhập kèm role đảm nhiệm
        /// GET /api/members/by-user?userId=...
        /// </summary>
        [HttpGet("api/me/clubinfo")]
        public async Task<IActionResult> GetClubsByUser([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
                return BadRequest(new { success = false, message = "userId is required" });

            var clubs = await _service.GetMyClubsAsync(userId);
            return Ok(new { success = true, data = clubs });
        }

        // ─── Policy Endpoints ────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách policies được gán trực tiếp cho member
        /// GET /api/clubs/{clubId}/members/{memberId}/policies
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members/{memberId}/policies")]
        public async Task<IActionResult> GetMemberPolicies(int clubId, int memberId)
        {
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            var policies = await _policyService.GetUserDirectPoliciesAsync(member.UserId);
            return Ok(new { success = true, data = policies });
        }

        /// <summary>
        /// Gán policies cho member (thêm, không ghi đè)
        /// POST /api/clubs/{clubId}/members/{memberId}/policies
        /// Body: { "policyIds": [1, 2, 3] }
        /// </summary>
        [HttpPost("api/clubs/{clubId}/members/{memberId}/policies")]
        public async Task<IActionResult> AssignPolicies(int clubId, int memberId, [FromBody] MemberPolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            await _policyService.AssignPoliciesToUserAsync(member.UserId, dto.PolicyIds);

            var updated = await _policyService.GetUserDirectPoliciesAsync(member.UserId);
            return Ok(new { success = true, message = "Policies assigned successfully", data = updated });
        }

        /// <summary>
        /// Ghi đè toàn bộ policies của member
        /// PUT /api/clubs/{clubId}/members/{memberId}/policies
        /// Body: { "policyIds": [1, 2] }
        /// </summary>
        [HttpPut("api/clubs/{clubId}/members/{memberId}/policies")]
        public async Task<IActionResult> SetPolicies(int clubId, int memberId, [FromBody] MemberPolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            await _policyService.SetUserPoliciesAsync(member.UserId, dto.PolicyIds);

            var updated = await _policyService.GetUserDirectPoliciesAsync(member.UserId);
            return Ok(new { success = true, message = "Policies updated successfully", data = updated });
        }

        /// <summary>
        /// Thu hồi một policy khỏi member
        /// DELETE /api/clubs/{clubId}/members/{memberId}/policies/{policyId}
        /// </summary>
        [HttpDelete("api/clubs/{clubId}/members/{memberId}/policies/{policyId}")]
        public async Task<IActionResult> RevokePolicy(int clubId, int memberId, int policyId)
        {
            var member = await _service.GetMemberByIdAsync(memberId);
            if (member == null || member.ClubId != clubId)
                return NotFound(new { success = false, message = "Member not found" });

            var revoked = await _policyService.RevokePolicyFromUserAsync(member.UserId, policyId);
            if (!revoked)
                return NotFound(new { success = false, message = "Policy not assigned to this member" });

            return Ok(new { success = true, message = "Policy revoked successfully" });
        }

        /// <summary>
        /// Lấy danh sách phòng ban mà thành viên ĐÃ tham gia (dùng cho modal xóa khỏi phòng ban)
        /// GET /api/clubs/{clubId}/members/{memberId}/departments/joined
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members/{memberId}/departments/joined")]
        public async Task<IActionResult> GetJoinedDepartments(int clubId, int memberId)
        {
            var departments = await _departmentService.GetDepartmentsJoinedByMemberAsync(clubId, memberId);
            if (departments == null)
                return NotFound(new { success = false, message = "Member not found in this club" });
            return Ok(new { success = true, data = departments });
        }

        /// <summary>
        /// Lấy danh sách phòng ban mà thành viên CHƯA tham gia (dùng cho modal thêm vào phòng ban)
        /// GET /api/clubs/{clubId}/members/{memberId}/departments/not-joined
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members/{memberId}/departments/not-joined")]
        public async Task<IActionResult> GetNotJoinedDepartments(int clubId, int memberId)
        {
            var departments = await _departmentService.GetDepartmentsNotJoinedByMemberAsync(clubId, memberId);
            if (departments == null)
                return NotFound(new { success = false, message = "Member not found in this club" });
            return Ok(new { success = true, data = departments });
        }
    }
}
