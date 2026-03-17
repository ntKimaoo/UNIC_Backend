using DataAccess.Seed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;

namespace UNIC.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpGet("test-user-id")]
        public IActionResult GetTestUserId()
        {
            return Ok(new { userId = ApplicationDemoSeeder.TestUserId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllApplications([FromQuery] int clubId)
        {
            var applications = await _applicationService.GetAllApplicationsAsync(clubId);
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetApplicationById(int id, [FromQuery] int clubId)
        {
            var application = await _applicationService.GetApplicationByIdAsync(id, clubId);
            if (application == null)
                return NotFound(new { success = false });

            return Ok(new { success = true, data = application });
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetApplicationsByUser(Guid userId, [FromQuery] int clubId)
        {
            var applications = await _applicationService.GetApplicationsByUserAsync(userId, clubId);
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("form/{formId:int}")]
        public async Task<IActionResult> GetApplicationsByForm(int formId, [FromQuery] int clubId)
        {
            var applications = await _applicationService.GetApplicationsByFormAsync(formId, clubId);
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetApplicationsByStatus(string status, [FromQuery] int clubId)
        {
            var applications = await _applicationService.GetApplicationsByStatusAsync(status, clubId);
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("user/{userId:guid}/form/{formId:int}")]
        public async Task<IActionResult> GetApplicationByUserAndForm(Guid userId, int formId, [FromQuery] int clubId)
        {
            var application = await _applicationService.GetApplicationByUserAndFormAsync(userId, formId, clubId);
            if (application == null)
                return NotFound(new { success = false });

            return Ok(new { success = true, data = application });
        }

        [HttpGet("campaign/{campaignId:int}")]
        public async Task<IActionResult> GetApplicationsByCampaign(int campaignId, [FromQuery] int clubId, [FromQuery] string? status = null)
        {
            var applications = await _applicationService.GetApplicationsByCampaignAsync(campaignId, clubId, status);
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("club/{clubId:int}")]
        public async Task<IActionResult> GetApplicationsByClub(int clubId, [FromQuery] string? status = null)
        {
            var applications = await _applicationService.GetApplicationsByClubAsync(clubId, status);
            return Ok(new { success = true, data = applications });
        }

        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationDto request)
        {
            var created = await _applicationService.CreateApplicationAsync(request);
            return Ok(new { success = true, data = created });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromQuery] int clubId, [FromBody] ApplicationResponseDto application)
        {
            var result = await _applicationService.UpdateApplicationAsync(id, clubId, application);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromQuery] int clubId, [FromBody] UpdateApplicationStatusDto request)
        {
            try
            {
                var updated = await _applicationService.UpdateApplicationStatusAsync(id, clubId, request.Status);
                if (updated == null)
                    return NotFound(new { success = false });

                return Ok(new { success = true, data = updated });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteApplication(int id, [FromQuery] int clubId)
        {
            var result = await _applicationService.DeleteApplicationAsync(id, clubId);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        // ================= FORM =================

        [HttpGet("forms")]
        public async Task<IActionResult> GetAllForms([FromQuery] int clubId)
        {
            var forms = await _applicationService.GetAllFormsAsync(clubId);
            return Ok(new { success = true, data = forms });
        }

        [HttpGet("forms/campaign/{campaignId:int}")]
        public async Task<IActionResult> GetFormsByCampaign(int campaignId, [FromQuery] int clubId)
        {
            var forms = await _applicationService.GetFormsByCampaignAsync(campaignId, clubId);
            return Ok(new { success = true, data = forms });
        }

        [HttpGet("forms/{id:int}")]
        public async Task<IActionResult> GetFormById(int id, [FromQuery] int clubId)
        {
            var form = await _applicationService.GetFormByIdAsync(id, clubId);
            if (form == null)
                return NotFound(new { success = false });

            return Ok(new { success = true, data = form });
        }

        [HttpPost("forms")]
        public async Task<IActionResult> CreateForm([FromBody] CreateApplicationFormDto request)
        {
            var created = await _applicationService.CreateFormAsync(request);
            return Ok(new { success = true, data = created });
        }

        [HttpPut("forms/{id:int}")]
        public async Task<IActionResult> UpdateForm(int id, [FromQuery] int clubId, [FromBody] ApplicationFormResponseDto form)
        {
            var result = await _applicationService.UpdateFormAsync(id, clubId, form);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        [HttpDelete("forms/{id:int}")]
        public async Task<IActionResult> DeleteForm(int id, [FromQuery] int clubId)
        {
            var result = await _applicationService.DeleteFormAsync(id, clubId);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        // ================= QUESTION =================

        [HttpGet("forms/{formId:int}/questions")]
        public async Task<IActionResult> GetQuestionsByForm(int formId, [FromQuery] int clubId)
        {
            var questions = await _applicationService.GetQuestionsByFormAsync(formId, clubId);
            return Ok(new { success = true, data = questions });
        }

        [HttpGet("questions/{id:int}")]
        public async Task<IActionResult> GetQuestionById(int id, [FromQuery] int clubId)
        {
            var question = await _applicationService.GetQuestionByIdAsync(id, clubId);
            if (question == null)
                return NotFound(new { success = false });

            return Ok(new { success = true, data = question });
        }

        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateApplicationQuestionDto request)
        {
            var created = await _applicationService.CreateQuestionAsync(request);
            return Ok(new { success = true, data = created });
        }

        [HttpPut("questions/{id:int}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromQuery] int clubId, [FromBody] ApplicationQuestionResponseDto question)
        {
            var result = await _applicationService.UpdateQuestionAsync(id, clubId, question);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        [HttpDelete("questions/{id:int}")]
        public async Task<IActionResult> DeleteQuestion(int id, [FromQuery] int clubId)
        {
            var result = await _applicationService.DeleteQuestionAsync(id, clubId);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        // ================= ANSWER =================

        [HttpGet("{applicationId:int}/answers")]
        public async Task<IActionResult> GetAnswersByApplication(int applicationId, [FromQuery] int clubId)
        {
            var answers = await _applicationService.GetAnswersByApplicationAsync(applicationId, clubId);
            return Ok(new { success = true, data = answers });
        }

        [HttpGet("answers/{id:int}")]
        public async Task<IActionResult> GetAnswerById(int id, [FromQuery] int clubId)
        {
            var answer = await _applicationService.GetAnswerByIdAsync(id, clubId);
            if (answer == null)
                return NotFound(new { success = false });

            return Ok(new { success = true, data = answer });
        }

        [HttpPost("{applicationId:int}/answers")]
        public async Task<IActionResult> CreateAnswer(int applicationId, [FromBody] CreateApplicationAnswerDto request, [FromQuery] int clubId)
        {
            request.ApplicationId = applicationId;

            try
            {
                var created = await _applicationService.CreateAnswerAsync(request, clubId);
                return Ok(new { success = true, data = created });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitApplication([FromQuery] int clubId, [FromBody] SubmitApplicationWithAnswersDto request)
        {
            try
            {
                var created = await _applicationService.SubmitApplicationWithAnswersAsync(clubId, request);
                return Ok(new { success = true, data = created });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("answers/{id:int}")]
        public async Task<IActionResult> UpdateAnswer(int id, [FromQuery] int clubId, [FromBody] ApplicationAnswerResponseDto answer)
        {
            var result = await _applicationService.UpdateAnswerAsync(id, clubId, answer);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }

        [HttpDelete("answers/{id:int}")]
        public async Task<IActionResult> DeleteAnswer(int id, [FromQuery] int clubId)
        {
            var result = await _applicationService.DeleteAnswerAsync(id, clubId);
            if (!result)
                return NotFound(new { success = false });

            return Ok(new { success = true });
        }
    }
}
