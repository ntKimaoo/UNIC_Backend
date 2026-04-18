using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using UNIC.BusinessLogic.DTOs;

namespace UNIC.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClubCreationRequestController : ControllerBase
    {
        private readonly IClubCreationRequestService _service;

        public ClubCreationRequestController(IClubCreationRequestService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _service.GetAllAsync();
            return Ok(new { success = true, data = requests });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);

            if (request == null)
                return NotFound();

            return Ok(new { success = true, data = request });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var requests = await _service.GetByUserIdAsync(userId);
            return Ok(new { success = true, data = requests });
        }

        [HttpGet("user/{userId}/has-pending")]
        public async Task<IActionResult> HasPending(Guid userId)
        {
            var hasPending = await _service.HasPendingRequestAsync(userId);
            return Ok(new { success = true, data = hasPending });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClubCreationRequestDto dto)
        {
            await _service.CreateAsync(dto);
            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClubCreationRequestDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            if (!result)
                return NotFound();

            return Ok(new { success = true });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateClubRequestStatusDto dto)
        {
            var result = await _service.UpdateStatusAsync(id, dto);

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Request not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Status updated successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { success = true });
        }
    }
}