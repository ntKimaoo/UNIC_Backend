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
        /// Get all policy groups
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var policies = await _service.GetAllPolicyGroupAsync();
            return Ok(new { success = true, data = policies });
        }

        /// <summary>
        /// Get all policies by group id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAllGroupById(int id)
        {
            var policies = await _service.GetAllPoliciesByGroupAsync(id);
            return Ok(new { success = true, data = policies });
        }

        /// <summary>
        /// Get all policies of a user (direct + role + club)
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPolicies(Guid userId)
        {
            var policies = await _service.GetUserPoliciesAsync(userId);

            return Ok(new { success = true, data = policies });
        }

        /// <summary>
        /// Get policies assigned directly to user
        /// </summary>
        [HttpGet("user/{userId}/direct")]
        public async Task<IActionResult> GetUserDirectPolicies(Guid userId)
        {
            var policies = await _service.GetUserDirectPoliciesAsync(userId);

            return Ok(new { success = true, data = policies });
        }

        /// <summary>
        /// Check if user has a specific policy
        /// </summary>
        [HttpGet("user/{userId}/check/{policyTitle}")]
        public async Task<IActionResult> HasPolicy(Guid userId, string policyTitle)
        {
            var result = await _service.HasUserPolicyAsync(userId, policyTitle);

            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Assign policies to user
        /// </summary>
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AssignPolicies(Guid userId, [FromBody] IEnumerable<int> policyIds)
        {
            await _service.AssignPoliciesToUserAsync(userId, policyIds);

            return Ok(new { success = true, message = "Policies assigned successfully" });
        }

        /// <summary>
        /// Revoke policy from user
        /// </summary>
        [HttpDelete("user/{userId}/{policyId}")]
        public async Task<IActionResult> RevokePolicy(Guid userId, int policyId)
        {
            var result = await _service.RevokePolicyFromUserAsync(userId, policyId);

            if (!result)
            {
                return NotFound(new { success = false, message = "Policy assignment not found" });
            }

            return Ok(new { success = true, message = "Policy revoked successfully" });
        }

        /// <summary>
        /// Replace all policies of user
        /// </summary>
        [HttpPut("user/{userId}")]
        public async Task<IActionResult> SetUserPolicies(Guid userId, [FromBody] IEnumerable<int> policyIds)
        {
            await _service.SetUserPoliciesAsync(userId, policyIds);

            return Ok(new { success = true, message = "User policies updated successfully" });
        }
    }
}