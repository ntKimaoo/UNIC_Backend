using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class RoomsControllerTest
    {
        private readonly Mock<IInterviewService> _mockService;
        private readonly RoomsController _controller;

        public RoomsControllerTest()
        {
            _mockService = new Mock<IInterviewService>();
            _controller = new RoomsController(_mockService.Object);
        }

        #region JoinRoom

        [Fact]
        public async Task JoinRoom_ReturnsOk_WhenSuccess()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid(), DisplayName = "User1" };
            _mockService.Setup(s => s.JoinRoomAsync("ROOM1", dto))
                .ReturnsAsync(new JoinRoomResponseDto { RoomCode = "ROOM1", RoomStatus = "Active" });

            var result = await _controller.JoinRoom("ROOM1", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsNotFound_WhenKeyNotFound()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid(), DisplayName = "U" };
            _mockService.Setup(s => s.JoinRoomAsync("NOROOM", dto))
                .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.JoinRoom("NOROOM", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task JoinRoom_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new JoinRoomDto { UserId = Guid.NewGuid(), DisplayName = "U" };
            _mockService.Setup(s => s.JoinRoomAsync("ROOM1", dto))
                .ThrowsAsync(new Exception("Already in room"));

            var result = await _controller.JoinRoom("ROOM1", dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region LeaveRoom

        [Fact]
        public async Task LeaveRoom_ReturnsOk_WhenSuccess()
        {
            var dto = new LeaveRoomDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.LeaveRoomAsync("ROOM1", dto)).ReturnsAsync(true);

            var result = await _controller.LeaveRoom("ROOM1", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LeaveRoom_ReturnsNotFound_WhenMissing()
        {
            var dto = new LeaveRoomDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.LeaveRoomAsync("ROOM1", dto)).ReturnsAsync(false);

            var result = await _controller.LeaveRoom("ROOM1", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetParticipants

        [Fact]
        public async Task GetParticipants_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetParticipantsAsync("ROOM1"))
                .ReturnsAsync(new List<RoomParticipantResponseDto> { new() });

            var result = await _controller.GetParticipants("ROOM1");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetParticipants_ReturnsNotFound_WhenKeyNotFound()
        {
            _mockService.Setup(s => s.GetParticipantsAsync("NOROOM"))
                .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.GetParticipants("NOROOM");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetEvents

        [Fact]
        public async Task GetEvents_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetEventsAsync("ROOM1"))
                .ReturnsAsync(new List<RoomEventResponseDto> { new() });

            var result = await _controller.GetEvents("ROOM1");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvents_ReturnsNotFound_WhenKeyNotFound()
        {
            _mockService.Setup(s => s.GetEventsAsync("NOROOM"))
                .ThrowsAsync(new KeyNotFoundException("Room not found"));

            var result = await _controller.GetEvents("NOROOM");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region CloseRoom

        [Fact]
        public async Task CloseRoom_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.CloseRoomAsync("ROOM1")).ReturnsAsync(true);

            var result = await _controller.CloseRoom("ROOM1");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CloseRoom_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.CloseRoomAsync("NOROOM")).ReturnsAsync(false);

            var result = await _controller.CloseRoom("NOROOM");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
