using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class AttendanceServiceTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventRepository> _mockEventRepo;
        private readonly Mock<IAttendanceRepository> _mockAttendanceRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AttendanceService _attendanceService;

        public AttendanceServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventRepo = new Mock<IEventRepository>();
            _mockAttendanceRepo = new Mock<IAttendanceRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepo.Object);
            _mockUnitOfWork.Setup(u => u.Attendances).Returns(_mockAttendanceRepo.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

            _attendanceService = new AttendanceService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        #region RegisterMemberAsync

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowNotFoundException_WhenEventNotFound()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _attendanceService.RegisterMemberAsync(request));
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowDomainException_WhenStatusNotOpen()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1 };
            var ev = new Event { EventId = 1, Status = "CLOSED" };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() => _attendanceService.RegisterMemberAsync(request));
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowDomainException_WhenTooEarly()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1 };
            var ev = new Event { EventId = 1, Status = "REGISTRATION_OPEN", RegistrationStartDate = DateTime.Now.AddDays(1) };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.RegisterMemberAsync(request));
            Assert.Contains("Registration has not started", ex.Message);
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowDomainException_WhenTooLate()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1 };
            var ev = new Event { EventId = 1, Status = "REGISTRATION_OPEN", RegistrationEndDate = DateTime.Now.AddDays(-1) };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.RegisterMemberAsync(request));
            Assert.Contains("Registration has ended", ex.Message);
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowConflictException_WhenAlreadyRegistered()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var ev = new Event { EventId = 1, Status = "REGISTRATION_OPEN" };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.IsUserRegisteredAsync(1, request.UserId)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _attendanceService.RegisterMemberAsync(request));
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldThrowDomainException_WhenMaxCapacityReached()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var ev = new Event { EventId = 1, Status = "REGISTRATION_OPEN", MaxAttendees = 10 };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.IsUserRegisteredAsync(1, request.UserId)).ReturnsAsync(false);
            _mockEventRepo.Setup(r => r.GetAttendeeCountAsync(1)).ReturnsAsync(10); // Reached max

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.RegisterMemberAsync(request));
            Assert.Contains("maximum capacity", ex.Message);
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldAddAttendanceAndSave()
        {
            // Arrange
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var ev = new Event { EventId = 1, Status = "REGISTRATION_OPEN" }; // No max capacity
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.IsUserRegisteredAsync(1, request.UserId)).ReturnsAsync(false);
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockAttendanceRepo.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _attendanceService.RegisterMemberAsync(request);

            // Assert
            _mockAttendanceRepo.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GenerateCheckInCodeAsync

        [Fact]
        public async Task GenerateCheckInCodeAsync_ShouldThrow_WhenEventNotFound()
        {
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _attendanceService.GenerateCheckInCodeAsync(1));
        }

        [Fact]
        public async Task GenerateCheckInCodeAsync_ShouldUpdateEventAndReturnCode()
        {
            // Arrange
            var ev = new Event { EventId = 1 };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            
            var expectedResponse = new CheckInCodeResponse { Code = "123456" };
            _mockMapper.Setup(m => m.Map<CheckInCodeResponse>(ev)).Returns(expectedResponse);

            // Act
            var result = await _attendanceService.GenerateCheckInCodeAsync(1);

            // Assert
            Assert.NotNull(ev.CheckInCode);
            Assert.Equal(6, ev.CheckInCode.Length);
            Assert.True(ev.CodeExpiresAt.HasValue);
            
            _mockEventRepo.Verify(r => r.Update(ev), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            Assert.Equal(expectedResponse, result);
        }

        #endregion

        #region CheckInMemberAsync

        [Fact]
        public async Task CheckInMemberAsync_ShouldThrow_WhenEventNotFound()
        {
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _attendanceService.CheckInMemberAsync(new CheckInRequest()));
        }

        [Fact]
        public async Task CheckInMemberAsync_ShouldThrow_WhenCodeIsInvalid()
        {
            var ev = new Event { EventId = 1, CheckInCode = "VALID1" };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            
            var request = new CheckInRequest { EventId = 1, Code = "INVALID" };
            
            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.CheckInMemberAsync(request));
            Assert.Contains("Invalid check-in code", ex.Message);
        }

        [Fact]
        public async Task CheckInMemberAsync_ShouldThrow_WhenCodeIsExpired()
        {
            var ev = new Event { EventId = 1, CheckInCode = "VALID1", CodeExpiresAt = DateTime.Now.AddMinutes(-5) };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            
            var request = new CheckInRequest { EventId = 1, Code = "VALID1" };
            
            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.CheckInMemberAsync(request));
            Assert.Contains("has expired", ex.Message);
        }

        [Fact]
        public async Task CheckInMemberAsync_ShouldThrow_WhenNotRegistered()
        {
            var request = new CheckInRequest { EventId = 1, UserId = Guid.NewGuid(), Code = "VALID1" };
            var ev = new Event { EventId = 1, CheckInCode = "VALID1", CodeExpiresAt = DateTime.Now.AddMinutes(5) };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.GetByEventAndUserAsync(1, request.UserId)).ReturnsAsync((Attendance?)null);
            
            await Assert.ThrowsAsync<NotFoundException>(() => _attendanceService.CheckInMemberAsync(request));
        }

        [Fact]
        public async Task CheckInMemberAsync_ShouldUpdateAttendanceAndSave()
        {
            var request = new CheckInRequest { EventId = 1, UserId = Guid.NewGuid(), Code = "VALID1" };
            var ev = new Event { EventId = 1, CheckInCode = "VALID1", CodeExpiresAt = DateTime.Now.AddMinutes(5) };
            var attendance = new Attendance { AttendanceStatus = "REGISTERED" };

            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.GetByEventAndUserAsync(1, request.UserId)).ReturnsAsync(attendance);
            
            await _attendanceService.CheckInMemberAsync(request);

            Assert.Equal("PRESENT", attendance.AttendanceStatus);
            Assert.True(attendance.CheckInTime.HasValue);
            _mockAttendanceRepo.Verify(r => r.Update(attendance), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region EvaluateMemberAsync

        [Fact]
        public async Task EvaluateMemberAsync_ShouldThrow_WhenEventNotEnded()
        {
            var request = new EvaluateMemberRequest { EventId = 1 };
            var ev = new Event { EventId = 1, EndDate = DateTime.Now.AddHours(1) };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.EvaluateMemberAsync(request));
            Assert.Contains("before event has ended", ex.Message);
        }

        [Fact]
        public async Task EvaluateMemberAsync_ShouldThrow_WhenMemberNotPresent()
        {
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid() };
            var ev = new Event { EventId = 1, EndDate = DateTime.Now.AddHours(-1) };
            var attendance = new Attendance { AttendanceStatus = "ABSENT" };
            
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.GetByEventAndUserAsync(1, request.UserId)).ReturnsAsync(attendance);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _attendanceService.EvaluateMemberAsync(request));
            Assert.Contains("must have attended", ex.Message);
        }

        [Fact]
        public async Task EvaluateMemberAsync_ShouldUpdateAttendance()
        {
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 5, Comment = "Good" };
            var ev = new Event { EventId = 1, EndDate = DateTime.Now.AddHours(-1) };
            var attendance = new Attendance { AttendanceStatus = "PRESENT" };
            
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockAttendanceRepo.Setup(r => r.GetByEventAndUserAsync(1, request.UserId)).ReturnsAsync(attendance);

            await _attendanceService.EvaluateMemberAsync(request);

            Assert.Equal(5, attendance.Score);
            Assert.Equal("Good", attendance.Comment);
            _mockAttendanceRepo.Verify(r => r.Update(attendance), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetEventAttendeesAsync

        [Fact]
        public async Task GetEventAttendeesAsync_ShouldThrow_WhenEventNotFound()
        {
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _attendanceService.GetEventAttendeesAsync(1));
        }

        [Fact]
        public async Task GetEventAttendeesAsync_ShouldMapAndReturnDtos()
        {
            var ev = new Event { EventId = 1 };
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var attendances = new List<Attendance> { new Attendance { EventId = 1, UserId = Guid.NewGuid() } };
            _mockAttendanceRepo.Setup(r => r.GetAttendeesByEventAsync(1)).ReturnsAsync(attendances);

            var expectedDtos = new List<AttendanceDetailDto> { new AttendanceDetailDto() };
            _mockMapper.Setup(m => m.Map<IEnumerable<AttendanceDetailDto>>(attendances)).Returns(expectedDtos);

            var result = await _attendanceService.GetEventAttendeesAsync(1);

            Assert.Equal(expectedDtos, result);
        }

        #endregion
    }
}
