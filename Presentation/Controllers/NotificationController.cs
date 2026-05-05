using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var notifications = await _notificationService.GetByUserIdAsync(userId);
            return Ok(new { success = true, data = notifications });
        }

        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var result = await _notificationService.MarkAsReadAsync(notificationId);
            if (!result) return NotFound(new { message = "Không tìm thấy thông báo." });
            return Ok(new { message = "Đã đánh dấu là đã đọc." });
        }

        [HttpPatch("{userId}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(Guid userId)
        {
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc." });
        }
        [HttpPost("send")]
        [RequireClubPolicyOrRole("CreateNotification", "Admin")]
        public async Task<IActionResult> SendNotification(Guid userId, string title, string message, string type)
        {
            await _notificationService.SendNotificationAsync(userId, title, message, type);
            return Ok(new { message = "Đã gửi thông báo." });
        }
        [HttpPost("send-bulk")]
        [RequireClubPolicyOrRole("CreateNotification", "Admin")]
        public async Task<IActionResult> SendBulkNotification([FromBody] BulkNotificationDto dto)
        {
            foreach (var userId in dto.UserIds)
                await _notificationService.SendNotificationAsync(userId, dto.Title, dto.Message, dto.Type);
            return Ok(new { message = "Đã gửi thông báo." });
        }
    }
}
