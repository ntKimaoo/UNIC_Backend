using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    
    public class ClubMemberController : ControllerBase
    {
        private readonly IClubMemberService _service;

        public ClubMemberController(IClubMemberService service)
        {
            _service = service;
        }

        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? User.FindFirst("userId");

            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy danh sách members của club
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members")]
        
        public async Task<IActionResult> GetMembers(int clubId)
        {
            var members = await _service.GetMembersByClubAsync(clubId);
            return Ok(new { success = true, data = members });
        }

        /// <summary>
        /// Lấy thông tin một member theo ID
        /// </summary>
        [HttpGet("api/clubs/{clubId}/members{memberId}")]
       
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
        
        public async Task<IActionResult> UpdateMemberRole(int clubId, int memberId, [FromBody] UpdateMemberRoleDto dto)
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

        /// <summary>
        /// Lấy danh sách club mà user đã gia nhập kèm role đảm nhiệm
        /// GET /api/members/by-user?userId=...
        /// </summary>
        [HttpGet("me/clubinfo")]
        public async Task<IActionResult> GetClubsByUser([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
                return BadRequest(new { success = false, message = "userId is required" });

            var clubs = await _service.GetMyClubsAsync(userId);
            return Ok(new { success = true, data = clubs });
        }
    }
}
