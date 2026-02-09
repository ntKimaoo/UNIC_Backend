using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;

namespace UNIC.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            if (!departments.Any())
            {
                return NotFound(new { success = false, message = "No departments found" });
            }
            return Ok(new { success = true, data = departments });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound(new { success = false, message = "Department not found" });
            }
            return Ok(new { success = true, data = department });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentResponseDto department)
        {
            var result = await _departmentService.UpdateDepartmentAsync(id, department);
            if (!result)
            {
                return NotFound(new { success = false, message = "Department not found" });
            }

            // Ensure returned DTO reflects the id used for update
            department.DepartmentId = id;
            return Ok(new { success = true, data = department });
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto request)
        {
            var createdDepartment = await _departmentService.CreateDepartmentAsync(request);
            return CreatedAtAction(
                nameof(GetDepartmentById),
                new { id = createdDepartment.DepartmentId },
                new { success = true, data = createdDepartment }
            );
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var result = await _departmentService.DeleteDepartmentAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Department not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }
    }
}
