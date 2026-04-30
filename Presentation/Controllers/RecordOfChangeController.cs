using BusinessLogic.DTOs;
using BusinessLogic.Hubs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordOfChangeController : ControllerBase
    {
        private readonly IRecordOfChangeService _service;
        private readonly IRecordOfChangeHubContext _hubContext;

        public RecordOfChangeController(IRecordOfChangeService service, IRecordOfChangeHubContext hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Lấy danh sách lịch sử thay đổi có phân trang, lọc và tìm kiếm.
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định: 10)</param>
        /// <param name="search">Tìm kiếm trong EntityName, Notification, OldValue, NewValue, tên người thay đổi</param>
        /// <param name="entityName">Lọc theo loại entity (User, Club, Event, ...)</param>
        /// <param name="changeType">Lọc theo loại thay đổi: CREATE | UPDATE | DELETE</param>
        /// <param name="clubId">Lọc theo club</param>
        /// <param name="changedBy">Lọc theo UserId người thực hiện thay đổi</param>
        /// <param name="fromDate">Lọc từ ngày (inclusive)</param>
        /// <param name="toDate">Lọc đến ngày (inclusive)</param>
        /// <param name="oldValueSearch">Tìm kiếm riêng trong giá trị cũ (OldValue)</param>
        /// <param name="newValueSearch">Tìm kiếm riêng trong giá trị mới (NewValue)</param>
        [HttpGet]
        [RequireRole("Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? entityName = null,
            [FromQuery] string? changeType = null,
            [FromQuery] int? clubId = null,
            [FromQuery] Guid? changedBy = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? oldValueSearch = null,
            [FromQuery] string? newValueSearch = null)
        {
            try
            {
                var filter = new RecordOfChangeFilterDto
                {
                    PageNumber = pageNumber < 1 ? 1 : pageNumber,
                    PageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize,
                    Search = search,
                    EntityName = entityName,
                    ChangeType = changeType,
                    ClubId = clubId,
                    ChangedBy = changedBy,
                    FromDate = fromDate,
                    ToDate = toDate,
                    OldValueSearch = oldValueSearch,
                    NewValueSearch = newValueSearch
                };

                var (items, totalCount, totalPages) = await _service.GetPagedAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = items,
                    pageNumber = filter.PageNumber,
                    pageSize = filter.PageSize,
                    totalCount,
                    totalPages,
                    hasPreviousPage = filter.PageNumber > 1,
                    hasNextPage = filter.PageNumber < totalPages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
