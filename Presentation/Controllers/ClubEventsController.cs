using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Enums;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Presentation.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Club-scoped event management endpoints.
    /// Route: /api/club/{clubId}/events
    /// Permission: System Admin > Club Manager > EventCollaborator(role)
    /// </summary>
    [ApiController]
    [Route("api/club/{clubId}/events")]
    [Authorize]
    public class ClubEventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;

        public ClubEventsController(
            IEventService eventService,
            IFileStorageService fileStorageService,
            IUnitOfWork unitOfWork)
        {
            _eventService = eventService;
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
        }

        #region Permission Helpers

        private class ClubRoleClaimDto
        {
            public int ClubId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public int Level { get; set; }
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? User.FindFirst("UserId")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }

        private bool IsClubManager(int clubId)
        {
            if (User.IsInRole("Admin")) return true;
            var clubRolesClaim = User.FindFirst("club_roles")?.Value;
            if (string.IsNullOrEmpty(clubRolesClaim)) return false;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roles = JsonSerializer.Deserialize<List<ClubRoleClaimDto>>(clubRolesClaim, options);
                return roles != null && roles.Any(r => r.ClubId == clubId && (r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)));
            }
            catch { return false; }
        }

        /// <summary>
        /// Check permission: Admin > Club Manager > EventCollaborator(policy names)
        /// </summary>
        private async Task<bool> HasEventPermission(int clubId, int eventId, params string[] requiredPolicies)
        {
            if (User.IsInRole("Admin")) return true;
            if (IsClubManager(clubId)) return true;

            var userId = GetCurrentUserId();
            if (userId == null) return false;

            var collab = await _unitOfWork.EventMembers.GetByEventAndUserAsync(eventId, userId.Value);
            if (collab == null) return false;

            // If no specific policies required, just check that user is a collaborator
            if (requiredPolicies.Length == 0) return true;

            var rolePolicies = collab.EventRole?.EventRolePolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
            var memberPolicies = collab.EventMemberPolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
            var allUserPolicies = rolePolicies.Union(memberPolicies).Where(p => p != null).ToList();

            return requiredPolicies.Any(p => allUserPolicies.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check that user is any collaborator (no specific policy needed)
        /// </summary>
        private async Task<bool> IsEventCollaborator(int clubId, int eventId)
        {
            if (User.IsInRole("Admin")) return true;
            if (IsClubManager(clubId)) return true;
            var userId = GetCurrentUserId();
            if (userId == null) return false;
            var collab = await _unitOfWork.EventMembers.GetByEventAndUserAsync(eventId, userId.Value);
            return collab != null;
        }

        private ObjectResult Forbidden(string message = "Bạn không có quyền thực hiện hành động này.")
            => StatusCode(StatusCodes.Status403Forbidden, new { error = message });

        #endregion

        /// <summary>
        /// Create a new event for a club (Club Manager only — no eventId yet)
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> CreateEvent(int clubId, [FromForm] CreateEventRequest request, IFormFile? image)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return Forbidden("You do not have Manager permissions for this club.");

                request.ClubId = clubId;

                string? imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var creatorUserId = GetCurrentUserId();
                var eventDto = await _eventService.CreateEventAsync(request, imageUrl, creatorUserId);
                return CreatedAtAction("GetEventById", "Events", new { id = eventDto.EventId }, eventDto);
            }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "An error occurred", details = ex.Message }); }
        }

        /// <summary>
        /// Update an event — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [RequireEventPolicy("editevent")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int clubId, int id, [FromForm] UpdateEventRequest request, IFormFile? image)
        {
            try
            {

                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                if (image != null && image.Length > 0)
                {
                    request.ImageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.UpdateEventAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Upload image — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        [RequireEventPolicy("editevent")]
        public async Task<IActionResult> UploadEventImage(int clubId, int id, IFormFile image)
        {
            try
            {

                if (image == null || image.Length == 0) return BadRequest(new { error = "No image file provided." });

                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                await _eventService.UpdateEventAsync(new UpdateEventRequest
                {
                    EventId = id,
                    EventName = eventDto.EventName,
                    Description = eventDto.Description,
                    Location = eventDto.Location,
                    StartDate = eventDto.StartDate,
                    EndDate = eventDto.EndDate,
                    ImageUrl = imageUrl
                });
                return Ok(new { message = "Image uploaded successfully.", imageUrl });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Create session — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/sessions")]
        [RequireEventPolicy("managesession")]
        public async Task<ActionResult<SessionDto>> CreateSession(int clubId, int id, [FromBody] CreateSessionRequest request)
        {
            try
            {

                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var sessionDto = await _eventService.CreateSessionAsync(request);
                return Ok(sessionDto);
            }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }

        /// <summary>
        /// Update session — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPut("{id}/sessions/{scheduleId}")]
        [RequireEventPolicy("managesession")]
        public async Task<ActionResult<SessionDto>> UpdateSession(int clubId, int id, int scheduleId, [FromBody] UpdateSessionRequest request)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                request.ScheduleId = scheduleId;
                request.EventId    = id;
                var sessionDto = await _eventService.UpdateSessionAsync(request);
                return Ok(sessionDto);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (DomainException    ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }

        /// <summary>
        /// Delete session — CREATOR, MANAGER
        /// </summary>
        [HttpDelete("{id}/sessions/{scheduleId}")]
        [RequireEventPolicy("managesession")]
        public async Task<IActionResult> DeleteSession(int clubId, int id, int scheduleId)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _eventService.DeleteSessionAsync(scheduleId, id);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }

        /// <summary>
        /// Open registration — CREATOR, MANAGER
        /// </summary>
        [HttpPatch("{id}/open-registration")]
        [RequireEventPolicy("openregistration")]
        public async Task<ActionResult<EventDetailDto>> OpenRegistration(int clubId, int id, [FromBody] OpenRegistrationRequest request)
        {
            try
            {

                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var eventDto = await _eventService.OpenRegistrationAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Start event — CREATOR, MANAGER
        /// </summary>
        [HttpPut("{id}/start")]
        [RequireEventPolicy("startevent")]
        public async Task<IActionResult> StartEvent(int clubId, int id)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var result = await _eventService.StartEventAsync(id);
                return Ok(new { checkInCode = result.checkInCode, expiresAt = result.expiresAt });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Complete event — CREATOR, MANAGER
        /// </summary>
        [HttpPut("{id}/complete")]
        [RequireEventPolicy("completeevent")]
        public async Task<IActionResult> CompleteEvent(int clubId, int id)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _eventService.CompleteEventAsync(id);
                return Ok(new { message = "Event completed." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Get sessions — CREATOR, MANAGER, COORDINATOR, CHECKER
        /// </summary>
        [HttpGet("{id}/sessions")]
        public async Task<IActionResult> GetSessions(int clubId, int id)
        {
            try
            {
                if (!await IsEventCollaborator(clubId, id))
                    return Forbidden();

                var ev = await _eventService.GetEventByIdAsync(id);
                if (ev.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                return Ok(ev.Sessions ?? new List<SessionDto>());
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        #region Collaborator Management (Step 5)

        /// <summary>
        /// Get my role + permissions for this event
        /// </summary>
        /// <summary>
        /// Get my role (used by frontend to gate UI)
        /// </summary>
        [HttpGet("{id}/my-role")]
        public async Task<IActionResult> GetMyRole(int clubId, int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();

                var allPolicyNames = await _unitOfWork.EventRoles.GetEventPolicyNamesAsync();

                if (User.IsInRole("Admin") || IsClubManager(clubId))
                    return Ok(new { role = "ADMIN", policies = allPolicyNames });

                var collab = await _unitOfWork.EventMembers.GetByEventAndUserAsync(id, userId.Value);
                if (collab == null)
                    return Ok(new { role = (string?)null, policies = Array.Empty<string>() });

                var rolePolicies = collab.EventRole?.EventRolePolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
                var memberPolicies = collab.EventMemberPolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
                var allUserPolicies = rolePolicies.Union(memberPolicies).Where(p => p != null).Distinct().ToList();

                return Ok(new { role = collab.EventRole?.RoleName ?? "Member", policies = allUserPolicies });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        #region Event Roles Management

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetEventRoles(int clubId, int id)
        {
            try
            {
                if (!await IsEventCollaborator(clubId, id)) return Forbidden();

                var roles = await _unitOfWork.EventRoles.GetAllAsync(id);
                var result = roles.Select(r => new
                {
                    r.EventRoleId,
                    r.RoleName,
                    r.Description,
                    r.Level,
                    policies = r.EventRolePolicies?.Select(p => p.Policy?.Name ?? "") ?? Enumerable.Empty<string>()
                });
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/roles")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> CreateEventRole(int clubId, int id, [FromBody] EventRoleDto request)
        {
            try
            {

                if (await _unitOfWork.EventRoles.RoleNameExistsAsync(request.RoleName, id))
                    return BadRequest(new { error = "Role name already exists in this event." });

                var role = new global::DataAccess.Models.EventRole
                {
                    EventId = id,
                    RoleName = request.RoleName,
                    Description = request.Description,
                    Level = 2
                };
                var created = await _unitOfWork.EventRoles.CreateAsync(role);
                return Ok(new { success = true, data = created });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}/roles/{roleId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> UpdateEventRole(int clubId, int id, int roleId, [FromBody] EventRoleDto request)
        {
            try
            {

                var role = await _unitOfWork.EventRoles.GetByIdAsync(roleId, id);
                if (role == null) return NotFound(new { error = "Role not found." });

                if (role.Level == 1) return BadRequest(new { error = "Cannot modify the Creator role." });

                role.RoleName = request.RoleName;
                role.Description = request.Description;
                await _unitOfWork.EventRoles.UpdateAsync(role);

                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        public class EventRoleDto
        {
            public string RoleName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        [HttpDelete("{id}/roles/{roleId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> DeleteEventRole(int clubId, int id, int roleId)
        {
            try
            {

                var role = await _unitOfWork.EventRoles.GetByIdAsync(roleId, id);
                if (role == null) return NotFound(new { error = "Role not found." });

                if (role.Level == 1) return BadRequest(new { error = "Cannot delete the Creator role." });

                await _unitOfWork.EventRoles.DeleteAsync(roleId);
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}/roles/{roleId}/policies")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> SetEventRolePolicies(int clubId, int id, int roleId, [FromBody] List<string> policies)
        {
            try
            {

                var role = await _unitOfWork.EventRoles.GetByIdAsync(roleId, id);
                if (role == null) return NotFound(new { error = "Role not found." });
                if (role.Level == 1) return BadRequest(new { error = "Cannot modify Creator policies." });

                await _unitOfWork.EventRoles.SetPoliciesAsync(roleId, policies ?? new List<string>());
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        #endregion

        #region Event Members Management

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetEventMembers(int clubId, int id)
        {
            try
            {
                if (!await IsEventCollaborator(clubId, id)) return Forbidden();

                var members = await _unitOfWork.EventMembers.GetByEventIdAsync(id);
                var result = members.Select(m => new
                {
                    m.EventMemberId,
                    userId = m.UserId,
                    userName = m.User?.FullName ?? "Unknown",
                    userAvatar = m.User?.Avatar,
                    roleId = m.EventRoleId,
                    roleName = m.EventRole?.RoleName,
                    m.JoinDate,
                    m.Status,
                    rolePolicies = m.EventRole?.EventRolePolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>(),
                    customPolicies = m.EventMemberPolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>()
                });

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        public class AddEventMemberRequest
        {
            public Guid UserId { get; set; }
            public int? EventRoleId { get; set; }
        }

        [HttpPost("{id}/members")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> AddEventMember(int clubId, int id, [FromBody] AddEventMemberRequest request)
        {
            try
            {

                var existing = await _unitOfWork.EventMembers.GetByEventAndUserAsync(id, request.UserId);
                if (existing != null) return BadRequest(new { error = "User is already a member of this event." });

                var newMember = new global::DataAccess.Models.UserEventRole
                {
                    EventId = id,
                    UserId = request.UserId,
                    EventRoleId = request.EventRoleId,
                    JoinDate = DateTime.Now,
                    AssignedBy = GetCurrentUserId()
                };

                await _unitOfWork.EventMembers.AddAsync(newMember);
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}/members/{memberId}/role")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> UpdateEventMemberRole(int clubId, int id, int memberId, [FromBody] int? roleId)
        {
            try
            {

                var member = await _unitOfWork.EventMembers.GetByIdAsync(memberId);
                if (member == null || member.EventId != id) return NotFound(new { error = "Member not found." });

                if (member.EventRole?.Level == 1) return BadRequest(new { error = "Cannot change the Creator's role." });

                member.EventRoleId = roleId;
                await _unitOfWork.EventMembers.UpdateAsync(member);

                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}/members/{memberId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> RemoveEventMember(int clubId, int id, int memberId)
        {
            try
            {

                var member = await _unitOfWork.EventMembers.GetByIdAsync(memberId);
                if (member == null || member.EventId != id) return NotFound(new { error = "Member not found." });

                if (member.EventRole?.Level == 1) return BadRequest(new { error = "Cannot remove the Creator." });

                await _unitOfWork.EventMembers.DeleteAsync(memberId);
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}/members/{memberId}/policies")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> SetEventMemberPolicies(int clubId, int id, int memberId, [FromBody] List<string> policies)
        {
            try
            {

                var member = await _unitOfWork.EventMembers.GetByIdAsync(memberId);
                if (member == null || member.EventId != id) return NotFound(new { error = "Member not found." });

                if (member.EventRole?.Level == 1) return BadRequest(new { error = "Cannot modify Creator policies." });

                await _unitOfWork.EventMembers.SetMemberPoliciesAsync(memberId, policies ?? new List<string>());
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        #endregion

        #endregion
    }
}
