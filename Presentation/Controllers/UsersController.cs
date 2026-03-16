using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClubRoleService _clubRoleService;

        public UsersController(IUserService userService, IClubRoleService clubRoleService)
        {
            _userService = userService;
            _clubRoleService = clubRoleService;
        }

        // GET: api/users
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
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }
        // Get: api/users/all-clubs
        [HttpGet("{id}/all-clubs")]
        public async Task<IActionResult> GetAllClub(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }
            var result = await _userService.GetAllClubsById(id);
            if (result.IsNullOrEmpty())
            {
                return NotFound(new { success = false, message = "You have not join any club!" });
            }
            return Ok(new { success = true, data = result });
        }
        [HttpGet("/api/Users/{userId}/managed-clubs")]
        public async Task<IActionResult> GetManagedClubs(Guid userId)
        {
            var clubs = await _clubRoleService.GetManagedClubsAsync(userId);

            return Ok(new
            {
                success = true,
                data = clubs
            });
        }
    }
}