using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Presentation.Authorization;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;

namespace UNIC.Presentation.Controllers
{
    [Route("api/club/{clubId}/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        
        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        /// <summary>
        /// Get all departments for a specific club
        /// </summary>
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetDepartmentsByClub(int clubId)
        {
            var departments = await _departmentService.GetDepartmentsByClubIdAsync(clubId);
            if (!departments.Any())
            {
                return NotFound(new 
                { 
                    success = false, 
                    message = "No departments found for this club" 
                });
            }
            return Ok(new { success = true, data = departments });
        }

        /// <summary>
        /// Get a specific department by ID within a club
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(int clubId, int id)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null || department.ClubId != clubId)
            {
                return NotFound(new 
                { 
                    success = false, 
                    message = "Department not found in this club" 
                });
            }
            return Ok(new { success = true, data = department });
        }

        /// <summary>
        /// Create a new department for a club
        /// </summary>
        [HttpPost]
        [RequireClubPolicyOrRole("createdepartment")]
        public async Task<IActionResult> CreateDepartment(int clubId, [FromBody] CreateDepartmentDto request)
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
                var createdDepartment = await _departmentService.CreateDepartmentAsync(clubId, request);
                return CreatedAtAction(
                    nameof(GetDepartmentById),
                    new { clubId = clubId, id = createdDepartment.DepartmentId },
                    new { success = true, data = createdDepartment }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the department",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update a department in a club
        /// </summary>
        [HttpPut("{id}")]
        [RequireClubPolicyOrRole("editdepartment")]

        public async Task<IActionResult> UpdateDepartment(int clubId, int id, [FromBody] UpdateDepartmentDto request)
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
                var updatedDepartment = await _departmentService.UpdateDepartmentAsync(clubId, id, request);
                if (updatedDepartment == null)
                {
                    return NotFound(new 
                    { 
                        success = false, 
                        message = "Department not found in this club" 
                    });
                }

                return Ok(new 
                { 
                    success = true, 
                    message = "Department updated successfully",
                    data = updatedDepartment 
                });
            }
            
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the department",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a department from a club
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int clubId, int id)
        {
            var result = await _departmentService.DeleteDepartmentAsync(clubId, id);
            if (!result)
            {
                return NotFound(new 
                { 
                    success = false, 
                    message = "Department not found in this club" 
                });
            }
            return Ok(new 
            { 
                success = true, 
                message = "Department deleted successfully",
                data = new { id } 
            });
        }

        /// <summary>
        /// Get all members (and their club-wide role) that belong to a specific department.
        /// Route: GET api/club/{clubId}/department/{departmentId}/all-members
        /// </summary>
        [HttpGet("{departmentId}/all-members")]
        public async Task<IActionResult> GetDepartmentMembers(int clubId, int departmentId)
        {
            var members = await _departmentService.GetDepartmentMembersAsync(clubId, departmentId);

            if (members == null)
                return NotFound(new
                {
                    success = false,
                    message = "Department not found in this club"
                });

            return Ok(new
            {
                success = true,
                data = members
            });
        }
        ///<summary>
        ///Add member to department
        /// </summary>
        [HttpPost("{departmentId}/members/{memberId}/add")]
        public async Task<IActionResult> AddMemberToDepartment(int clubId,int departmentId,Guid userId)
        {
            var member = await _departmentService.AddMemberTodepartment(clubId, userId, departmentId);
            return Ok(member);
        }
        ///<summary>
        ///Remove member from department
        /// </summary>
        [HttpDelete("{departmentId}/members/{memberId}/remove}")]
        public async Task<IActionResult> RemoveMemberFromDepartment(int clubId, int departmentId, Guid userId)
        {
            var member = await _departmentService.RemoveMemberFromDepartment(clubId, userId, departmentId);
            return Ok(member);
        }
    }
}
