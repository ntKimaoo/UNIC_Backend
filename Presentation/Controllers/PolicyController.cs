using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace UNIC.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _service;

        public PolicyController(IPolicyService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all policies
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var policies = await _service.GetAllPolicyGroupAsync();
            return Ok(new { success = true, data = policies });
        }
        /// <summary>
        /// Get all policies
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAllGroupById(int id)
        {
            var policies = await _service.GetAllPoliciesByGroupAsync(id);
            return Ok(new { success = true, data = policies });
        }
    }
}
