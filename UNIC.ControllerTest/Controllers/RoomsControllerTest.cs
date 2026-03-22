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
    public class RoomsControllerTest
    {
        private readonly Mock<IInterviewService> _mockService;
        private readonly RoomsController _controller;

        public RoomsControllerTest()
        {
            _mockService = new Mock<IInterviewService>();
            _controller = new RoomsController(_mockService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region JoinRoom

        [Fact]
        public async Task JoinRoom_ReturnsOk_WhenSuccess()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid(), DisplayName = "User" };
            var response = new JoinRoomResponseDto { RoomCode = "abc-1234", PeerId = "peer1" };

            _mockService.Setup(s => s.JoinRoomAsync("abc-1234", dto))
                        .ReturnsAsync(response);

            var result = await _controller.JoinRoom("abc-1234", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsNotFound_WhenKeyNotFound()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.JoinRoomAsync("missing", dto))
                        .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.JoinRoom("missing", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid() };

            _mockService.Setup(s => s.JoinRoomAsync("full", dto))
                        .ThrowsAsync(new InvalidOperationException("Room đã đầy"));

            var result = await _controller.JoinRoom("full", dto);

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

        #endregion

        #region GetParticipants

        [Fact]
        public async Task GetParticipants_ReturnsOk_WhenFound()
        {
            var participants = new List<RoomParticipantResponseDto>
            {
                new RoomParticipantResponseDto { Id = 1, DisplayName = "User" }
            };

            _mockService.Setup(s => s.GetParticipantsAsync("abc-1234"))
                        .ReturnsAsync(participants);

            var result = await _controller.GetParticipants("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetParticipants_ReturnsNotFound_WhenKeyNotFound()
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
                new RoomEventResponseDto { Id = 1, EventType = "participant.joined" }
            };

            _mockService.Setup(s => s.GetEventsAsync("abc-1234"))
                        .ReturnsAsync(events);

            var result = await _controller.GetEvents("abc-1234");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvents_ReturnsNotFound_WhenKeyNotFound()
        {
            _mockService.Setup(s => s.GetEventsAsync("missing"))
                        .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.GetEvents("missing");

            Assert.IsType<NotFoundObjectResult>(result);
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
    }
}
