using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecruitmentCampaignController : ControllerBase
    {
        private readonly IRecruitmentCampaignService _service;

        public RecruitmentCampaignController(IRecruitmentCampaignService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all recruitment campaigns (all clubs)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var campaigns = await _service.GetAllAsync();
            return Ok(new
            {
                success = true,
                data = campaigns
            });
        }

        /// <summary>
        /// Get all recruitment campaigns for a specific club
        /// </summary>
        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetAllByClubId(int clubId)
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
        public async Task<IActionResult> GetById(int id)
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
        [RequireClubPolicyOrRole("CreateCampaign")]
        public async Task<IActionResult> Create([FromBody] CreateRecruitmentCampaignDto dto)
        {
            try
            {
                var campaign = await _service.CreateAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = campaign.CampaignId },
                    new
                    {
                        success = true,
                        message = "Recruitment campaign created successfully",
                        data = campaign
                    });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing recruitment campaign
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRecruitmentCampaignDto dto)
        {
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
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a recruitment campaign
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