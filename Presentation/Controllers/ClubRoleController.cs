using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("")]
    public class ClubRoleController : ControllerBase
    {
        private readonly IClubRoleService _service;

        public ClubRoleController(IClubRoleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all club roles
        /// </summary>
        [HttpGet("api/club/{clubId}/role")]
        public async Task<IActionResult> GetAll(int clubId)
        {
            var roles = await _service.GetAllAsync(clubId);
            return Ok(new { success = true, data = roles });
        }

        /// <summary>
        /// Get club role by ID
        /// </summary>
        [HttpGet("api/club/{clubId}/role/{id}")]
        public async Task<IActionResult> GetById(int id,int clubId)
        {
            var role = await _service.GetByIdAsync(id, clubId);
            if (role == null)
                return NotFound(new { success = false, message = "Club role not found." });

            return Ok(new { success = true, data = role });
        }
       
        /// <summary>
        /// Create a new club role
        /// </summary>
        [HttpPost("api/club/{clubId}/role")]
        public async Task<IActionResult> Create([FromBody] CreateClubRoleDto dto,int clubId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var role = await _service.CreateAsync(dto,clubId);
                role.clubId = clubId;
                return CreatedAtAction(nameof(GetById), new { clubId = clubId, id = role.ClubRoleId }, new
                {
                    success = true,
                    message = "Club role created successfully",
                    data = role
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing club role
        /// </summary>
        [HttpPut("api/club/{clubId}/role/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClubRoleDto dto,int clubId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var role = await _service.UpdateAsync(id, dto,clubId);
                if (role == null)
                    return NotFound(new { success = false, message = "Club role not found" });

                return Ok(new { success = true, message = "Club role updated successfully", data = role });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }
        [HttpPut("api/club/{clubId}/role/{id}/policies")]
        public async Task<IActionResult> UpdatePolicies(int id, List<int> policyIds,int clubId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                await _service.UpdatePoliciesAsync(id, policyIds);
                return Ok(new { success = true, message = "Club role updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }
        /// <summary>
        /// Delete a club role
        /// </summary>
        [HttpDelete("api/club/{clubId}/role/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { success = false, message = "Club role not found" });

            return Ok(new { success = true, message = "Club role deleted successfully" });
        }
    }
}
