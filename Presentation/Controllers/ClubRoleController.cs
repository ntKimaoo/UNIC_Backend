using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _service.GetAllAsync();
            return Ok(new { success = true, data = roles });
        }

        /// <summary>
        /// Get club role by ID
        /// </summary>
        [HttpGet("{id}/policies")]
        public async Task<IActionResult> GetById(int id)
        {
            var role = await _service.GetPoliciesByRoleAsync(id);
            if (role == null)
                return NotFound(new { success = false, message = "This role haven't set up any poliecies yet." });

            return Ok(new { success = true, data = role });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPoliciesById(int id)
        {
            var role = await _service.GetByIdAsync(id);
            if (role == null)
                return NotFound(new { success = false, message = "Club role not found" });

            return Ok(new { success = true, data = role });
        }

        /// <summary>
        /// Create a new club role
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClubRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var role = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = role.ClubRoleId }, new
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
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClubRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var role = await _service.UpdateAsync(id, dto);
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
        [HttpPut("{id}/policies")]
        public async Task<IActionResult> UpdatePolicies(int id, List<int> policyIds)
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { success = false, message = "Club role not found" });

            return Ok(new { success = true, message = "Club role deleted successfully" });
        }

        /// <summary>
        /// Get all roles of a club
        /// </summary>
        [HttpGet("club/{id}")]
        public async Task<IActionResult> GetByClubId(int id)
        {
            var roles = await _service.GetRolesByClubIdAsync(id);
            return Ok(new { success = true, data = roles });
        }
    }
}
