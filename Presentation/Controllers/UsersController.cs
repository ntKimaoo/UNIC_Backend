using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Linq;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Uncomment nếu muốn chặn user chưa đăng nhập
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/users
        // Hỗ trợ OData: api/users?$select=fullName,email&$filter=status eq 'Active'
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            if (!users.Any())
            {
                return NotFound(new { success = false, message = "No users found" });
            }
            return Ok(new { success = true, data = users });
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            return Ok(new
            {
                success = true,
                data = user
            });
        }

        // POST: api/users
        [HttpPost]
        // [Authorize(Roles = "Admin")] // Thường chỉ Admin mới tạo được User trực tiếp
        public async Task<IActionResult> Create([FromBody] CreateUserDto request)
        {
            try
            {
                var createdUser = await _userService.CreateUserAsync(request);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdUser.UserId },
                    new { success = true, data = createdUser }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto request)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, request);
                if (!result)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                return Ok(new { success = true, data = new { id } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }
    }
}