using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class MeetingRoomsControllerTest
    {
        private readonly Mock<IInterviewService> _mockService;
        private readonly MeetingRoomsController _controller;

        public MeetingRoomsControllerTest()
        {
            _mockService = new Mock<IInterviewService>();
            _controller = new MeetingRoomsController(_mockService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateMeetingRoomDto { RoomType = "Online", MaxParticipants = 10 };
            var response = new MeetingRoomResponseDto { Id = 1, RoomCode = "abc-1234" };

            _mockService.Setup(s => s.CreateStandaloneRoomAsync(dto))
                        .ReturnsAsync(response);

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateMeetingRoomDto { RoomType = "Bad" };

            _mockService.Setup(s => s.CreateStandaloneRoomAsync(dto))
                        .ThrowsAsync(new Exception("Invalid room type"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("RoomType", "Required");

            var result = await _controller.Create(new CreateMeetingRoomDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var room = new MeetingRoomResponseDto { Id = 1, RoomCode = "abc-1234" };

            _mockService.Setup(s => s.GetRoomByIdAsync(1))
                        .ReturnsAsync(room);

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetRoomByIdAsync(99))
                        .ReturnsAsync((MeetingRoomResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetByCode

        [Fact]
        public async Task GetByCode_ReturnsOk_WhenFound()
        {
            var room = new MeetingRoomResponseDto { Id = 1, RoomCode = "abc-1234" };

            _mockService.Setup(s => s.GetRoomByCodeAsync("abc-1234"))
                        .ReturnsAsync(room);

            var result = await _controller.GetByCode("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetByCode_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetRoomByCodeAsync("invalid"))
                        .ReturnsAsync((MeetingRoomResponseDto?)null);

            var result = await _controller.GetByCode("invalid");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region JoinRoom

        [Fact]
        public async Task JoinRoom_ReturnsOk_WhenSuccess()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid() };
            var response = new JoinRoomResponseDto { RoomCode = "abc-1234", RoomStatus = "Active" };

            _mockService.Setup(s => s.JoinRoomAsync("abc-1234", dto))
                        .ReturnsAsync(response);

            var result = await _controller.JoinRoom("abc-1234", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsNotFound_WhenRoomMissing()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.JoinRoomAsync("missing", dto))
                        .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.JoinRoom("missing", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsBadRequest_WhenRoomClosedOrFull()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.JoinRoomAsync("full", dto))
                        .ThrowsAsync(new InvalidOperationException("Room đã đầy"));

            var result = await _controller.JoinRoom("full", dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("UserId", "Required");

            var result = await _controller.JoinRoom("abc", new JoinRoomDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region LeaveRoom

        [Fact]
        public async Task LeaveRoom_ReturnsOk_WhenSuccess()
        {
            var dto = new LeaveRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.LeaveRoomAsync("abc-1234", dto))
                        .ReturnsAsync(true);

            var result = await _controller.LeaveRoom("abc-1234", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LeaveRoom_ReturnsNotFound_WhenMissing()
        {
            var dto = new LeaveRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.LeaveRoomAsync("missing", dto))
                        .ReturnsAsync(false);

            var result = await _controller.LeaveRoom("missing", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task LeaveRoom_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("UserId", "Required");

            var result = await _controller.LeaveRoom("abc", new LeaveRoomDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region CloseRoom

        [Fact]
        public async Task CloseRoom_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.CloseRoomAsync("abc-1234"))
                        .ReturnsAsync(true);

            var result = await _controller.CloseRoom("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CloseRoom_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.CloseRoomAsync("missing"))
                        .ReturnsAsync(false);

            var result = await _controller.CloseRoom("missing");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetParticipants

        [Fact]
        public async Task GetParticipants_ReturnsOk_WhenFound()
        {
            var participants = new List<RoomParticipantResponseDto>
            {
                new RoomParticipantResponseDto { UserId = Guid.NewGuid() }
            };

            _mockService.Setup(s => s.GetParticipantsAsync("abc-1234"))
                        .ReturnsAsync(participants);

            var result = await _controller.GetParticipants("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetParticipants_ReturnsNotFound_WhenRoomMissing()
        {
            _mockService.Setup(s => s.GetParticipantsAsync("missing"))
                        .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.GetParticipants("missing");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetEvents

        [Fact]
        public async Task GetEvents_ReturnsOk_WhenFound()
        {
            var events = new List<RoomEventResponseDto>
            {
                new RoomEventResponseDto { EventType = "UserJoined" }
            };

            _mockService.Setup(s => s.GetEventsAsync("abc-1234"))
                        .ReturnsAsync(events);

            var result = await _controller.GetEvents("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvents_ReturnsNotFound_WhenRoomMissing()
        {
            _mockService.Setup(s => s.GetEventsAsync("missing"))
                        .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.GetEvents("missing");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
