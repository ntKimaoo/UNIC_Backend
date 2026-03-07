using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;

        public UsersController(IUserService userService, IFileStorageService fileStorageService)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1)
                return BadRequest(new { success = false, message = "Page must be >= 1" });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { success = false, message = "PageSize must be between 1 and 100" });

            var pagedResult = await _userService.GetPagedUsersAsync(page, pageSize);
            return Ok(new { success = true, data = pagedResult });
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

        // POST: api/users/{id}/avatar
        [HttpPost("{id}/avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(Guid id, IFormFile avatar)
        {
            try
            {
                if (avatar == null || avatar.Length == 0)
                    return BadRequest(new { success = false, message = "No file provided" });

                var imageUrl = await _fileStorageService.SaveFileAsync(avatar, "uniclub/avatars");
                var result = await _userService.UpdateUserAsync(id, new UpdateUserDto { Avatar = imageUrl });

                if (!result)
                    return NotFound(new { success = false, message = "User not found" });

                return Ok(new { success = true, data = new { avatarUrl = imageUrl } });
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
    }
}