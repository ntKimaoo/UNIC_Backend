using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/club/{clubId}/recruitment-campaign")]
    public class RecruitmentCampaignController : ControllerBase
    {
        private readonly IRecruitmentCampaignService _service;

        public RecruitmentCampaignController(IRecruitmentCampaignService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all recruitment campaigns for a specific club
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(int clubId)
        {
            var campaigns = await _service.GetByClubIdAsync(clubId);
            return Ok(new
            {
                success = true,
                data = campaigns
            });
        }

        /// <summary>
        /// Get recruitment campaign by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int clubId, int id)
        {
            var campaign = await _service.GetByIdAsync(id);
            if (campaign == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recruitment campaign not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = campaign
            });
        }

        /// <summary>
        /// Create a new recruitment campaign
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(int clubId, [FromBody] CreateRecruitmentCampaignDto dto)
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
                var campaign = await _service.CreateAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { clubId = clubId, id = campaign.CampaignId },
                    new
                    {
                        success = true,
                        message = "Recruitment campaign created successfully",
                        data = campaign
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing recruitment campaign
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int clubId, int id, [FromBody] UpdateRecruitmentCampaignDto dto)
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
                var campaign = await _service.UpdateAsync(id, dto);
                if (campaign == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Recruitment campaign not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Recruitment campaign updated successfully",
                    data = campaign
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a recruitment campaign
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int clubId, int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recruitment campaign not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Recruitment campaign deleted successfully"
            });
        }
    }
}

