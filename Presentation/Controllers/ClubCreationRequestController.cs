using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using UNIC.BusinessLogic.DTOs;

namespace UNIC.Presentation.Controllers
{
    [Authorize]
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
        [RequireRole("Admin")]
        public async Task<IActionResult> GetAll(int pageSize, string? searchQuery, string? status, string pageIndex)
        {
            try
            {
                var requests = await _service.GetAllAsync();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    var searchLower = searchQuery.ToLower();

                    requests = requests.Where(r =>
                        r.ClubName.ToLower().Contains(searchLower)
                    );
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var searchLower = status.ToLower();

                    requests = requests.Where(r =>
                        r.Status.ToLower().Contains(searchLower)
                    );
                }

                if (pageIndex.ToLower() != "all")
                {
                    var totalCount = requests.Count();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    if (int.TryParse(pageIndex, out int pageInt))
                    {
                        requests = requests
                            .Skip((pageInt - 1) * pageSize)
                            .Take(pageSize);

                        return Ok(new
                        {
                            success = true,
                            data = requests,
                            totalPages,
                            totalCount
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = requests,
                    totalPages = 1,
                    totalCount = requests.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [RequireRole("Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);

            if (request == null)
                return NotFound();

            return Ok(new { success = true, data = request });
        }

        [HttpGet("user/{userId}")]
        [RequireRole("User")]
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
        [RequireRole("User")]
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
        [RequireRole("Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateClubRequestStatusDto dto)
        {
            var request = await _service.UpdateStatusAsync(id, dto);

            if (request == null)
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