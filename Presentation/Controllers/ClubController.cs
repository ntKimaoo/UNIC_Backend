using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClubController : ControllerBase
    {
        private readonly IClubService _service;
        private readonly IClubRoleService _clubRoleService;

        public ClubController(IClubService service, IClubRoleService clubRoleService)
        {
            _service = service;
            _clubRoleService = clubRoleService;
        }

        /// <summary>
        /// Get all clubs (excluding soft-deleted)
        /// Requires "ViewClubs" policy
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clubs = await _service.GetAllAsync();
            return Ok(new
            {
                success = true,
                data = clubs
            });
        }

        /// <summary>
        /// Get all active clubs
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveClubs()
        {
            var clubs = await _service.GetActiveClubsAsync();
            return Ok(new
            {
                success = true,
                data = clubs
            });
        }

        /// <summary>
        /// Get all public clubs
        /// </summary>
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicClubs()
        {
            var clubs = await _service.GetPublicClubsAsync();
            return Ok(new
            {
                success = true,
                data = clubs
            });
        }

        /// <summary>
        /// Get club by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var club = await _service.GetByIdAsync(id);
            if (club == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Club not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = club
            });
        }

        /// <summary>
        /// Create a new club
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClubDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data",
                    errors = ModelState
                });
            }

            try
            {
                var club = await _service.CreateAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = club.ClubId },
                    new
                    {
                        success = true,
                        message = "Club created successfully",
                        data = club
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing club
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClubDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data",
                    errors = ModelState
                });
            }

            try
            {
                var club = await _service.UpdateAsync(id, dto);
                if (club == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Club not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Club updated successfully",
                    data = club
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the club",
                    error = ex.Message
                });
            }
        }
        /// <summary>
        /// Chang status of a club (active/inactive)
        /// </summary>
        [HttpPut("changeStatus/{id}")]
        public async Task<IActionResult> changStatus(int id)
        {
            try
            {
                await _service.ChangeStatusClub(id);
                return Ok(new
                {
                    success = true,
                    message = "Club updated successfully",
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Soft delete a club (mark as deleted)
        /// </summary>
        [HttpDelete("{id}/soft")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var result = await _service.SoftDeleteAsync(id);
            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Club not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Club soft deleted successfully"
            });
        }

        /// <summary>
        /// Permanently delete a club
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Club not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Club deleted permanently"
            });
        }

        /// <summary>
        /// Get the organizational structure of a club:
        /// standalone roles (no department) and departments with their manager and member roles
        /// </summary>
        [HttpGet("{clubId}/club-structure")]
        public async Task<IActionResult> GetClubStructure(int clubId)
        {
            var club = await _service.GetByIdAsync(clubId);
            if (club == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Club not found"
                });
            }

            var structure = await _clubRoleService.GetClubStructureAsync(clubId);
            return Ok(new
            {
                success = true,
                data = structure
            });
        }
    }
}
