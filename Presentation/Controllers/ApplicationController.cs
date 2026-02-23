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

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllApplications()
        {
            var applications = await _applicationService.GetAllApplicationsAsync();
            if (!applications.Any())
            {
                return NotFound(new { success = false, message = "No applications found" });
            }
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetApplicationById(int id)
        {
            var application = await _applicationService.GetApplicationByIdAsync(id);
            if (application == null)
            {
                return NotFound(new { success = false, message = "Application not found" });
            }
            return Ok(new { success = true, data = application });
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetApplicationsByUser(Guid userId)
        {
            var applications = await _applicationService.GetApplicationsByUserAsync(userId);
            if (!applications.Any())
            {
                return NotFound(new { success = false, message = "No applications found for user" });
            }
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("form/{formId:int}")]
        public async Task<IActionResult> GetApplicationsByForm(int formId)
        {
            var applications = await _applicationService.GetApplicationsByFormAsync(formId);
            if (!applications.Any())
            {
                return NotFound(new { success = false, message = "No applications found for form" });
            }
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetApplicationsByStatus(string status)
        {
            var applications = await _applicationService.GetApplicationsByStatusAsync(status);
            if (!applications.Any())
            {
                return NotFound(new { success = false, message = "No applications found for status" });
            }
            return Ok(new { success = true, data = applications });
        }

        [HttpGet("user/{userId:guid}/form/{formId:int}")]
        public async Task<IActionResult> GetApplicationByUserAndForm(Guid userId, int formId)
        {
            var application = await _applicationService.GetApplicationByUserAndFormAsync(userId, formId);
            if (application == null)
            {
                return NotFound(new { success = false, message = "Application not found for user and form" });
            }
            return Ok(new { success = true, data = application });
        }

        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationDto request)
        {
            var created = await _applicationService.CreateApplicationAsync(request);
            return CreatedAtAction(
                nameof(GetApplicationById),
                new { id = created.ApplicationId },
                new { success = true, data = created }
            );
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromBody] ApplicationResponseDto application)
        {
            var result = await _applicationService.UpdateApplicationAsync(id, application);
            if (!result)
            {
                return NotFound(new { success = false, message = "Application not found" });
            }

            application.ApplicationId = id;
            return Ok(new { success = true, data = application });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var result = await _applicationService.DeleteApplicationAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Application not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }

        [HttpGet("forms")]
        [EnableQuery]
        public async Task<IActionResult> GetAllForms()
        {
            var forms = await _applicationService.GetAllFormsAsync();
            if (!forms.Any())
            {
                return NotFound(new { success = false, message = "No forms found" });
            }
            return Ok(new { success = true, data = forms });
        }

        [HttpGet("forms/{id:int}")]
        public async Task<IActionResult> GetFormById(int id)
        {
            var form = await _applicationService.GetFormByIdAsync(id);
            if (form == null)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }
            return Ok(new { success = true, data = form });
        }

        [HttpPost("forms")]
        public async Task<IActionResult> CreateForm([FromBody] CreateApplicationFormDto request)
        {
            var created = await _applicationService.CreateFormAsync(request);
            return CreatedAtAction(
                nameof(GetFormById),
                new { id = created.FormId },
                new { success = true, data = created }
            );
        }

        [HttpPut("forms/{id:int}")]
        public async Task<IActionResult> UpdateForm(int id, [FromBody] ApplicationFormResponseDto form)
        {
            var result = await _applicationService.UpdateFormAsync(id, form);
            if (!result)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }

            form.FormId = id;
            return Ok(new { success = true, data = form });
        }

        [HttpDelete("forms/{id:int}")]
        public async Task<IActionResult> DeleteForm(int id)
        {
            var result = await _applicationService.DeleteFormAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Form not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }

        [HttpGet("forms/{formId:int}/questions")]
        public async Task<IActionResult> GetQuestionsByForm(int formId)
        {
            var questions = await _applicationService.GetQuestionsByFormAsync(formId);
            if (!questions.Any())
            {
                return NotFound(new { success = false, message = "No questions found for form" });
            }
            return Ok(new { success = true, data = questions });
        }

        [HttpGet("questions/{id:int}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var question = await _applicationService.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { success = false, message = "Question not found" });
            }
            return Ok(new { success = true, data = question });
        }

        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateApplicationQuestionDto request)
        {
            var created = await _applicationService.CreateQuestionAsync(request);
            return CreatedAtAction(
                nameof(GetQuestionById),
                new { id = created.QuestionId },
                new { success = true, data = created }
            );
        }

        [HttpPut("questions/{id:int}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] ApplicationQuestionResponseDto question)
        {
            var result = await _applicationService.UpdateQuestionAsync(id, question);
            if (!result)
            {
                return NotFound(new { success = false, message = "Question not found" });
            }

            question.QuestionId = id;
            return Ok(new { success = true, data = question });
        }

        [HttpDelete("questions/{id:int}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var result = await _applicationService.DeleteQuestionAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Question not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }

        //Application Answers
        [HttpGet("{applicationId:int}/answers")]
        public async Task<IActionResult> GetAnswersByApplication(int applicationId)
        {
            var answers = await _applicationService.GetAnswersByApplicationAsync(applicationId);
            if (!answers.Any())
            {
                return NotFound(new { success = false, message = "No answers found for this application" });
            }
            return Ok(new { success = true, data = answers });
        }

        [HttpGet("answers/{id:int}")]
        public async Task<IActionResult> GetAnswerById(int id)
        {
            var answer = await _applicationService.GetAnswerByIdAsync(id);
            if (answer == null)
            {
                return NotFound(new { success = false, message = "Answer not found" });
            }
            return Ok(new { success = true, data = answer });
        }

        [HttpPost("{applicationId:int}/answers")]
        public async Task<IActionResult> CreateAnswer(int applicationId, [FromBody] CreateApplicationAnswerDto request)
        {
            if (request.ApplicationId != applicationId)
            {
                request.ApplicationId = applicationId;
            }
            try
            {
                var created = await _applicationService.CreateAnswerAsync(request);
                return CreatedAtAction(
                    nameof(GetAnswerById),
                    new { id = created.AnswerId },
                    new { success = true, data = created }
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitApplicationWithAnswers([FromBody] SubmitApplicationWithAnswersDto request)
        {
            try
            {
                var created = await _applicationService.SubmitApplicationWithAnswersAsync(request);
                return CreatedAtAction(
                    nameof(GetApplicationById),
                    new { id = created.ApplicationId },
                    new { success = true, data = created }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("answers/{id:int}")]
        public async Task<IActionResult> UpdateAnswer(int id, [FromBody] ApplicationAnswerResponseDto answer)
        {
            var result = await _applicationService.UpdateAnswerAsync(id, answer);
            if (!result)
            {
                return NotFound(new { success = false, message = "Answer not found" });
            }
            answer.AnswerId = id;
            return Ok(new { success = true, data = answer });
        }

        [HttpDelete("answers/{id:int}")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var result = await _applicationService.DeleteAnswerAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Answer not found" });
            }
            return Ok(new { success = true, data = new { id } });
        }
    }
}
