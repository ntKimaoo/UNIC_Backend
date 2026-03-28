using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Enums;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class AttendanceServiceTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AttendanceService _attendanceService;

        public AttendanceServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailService = new Mock<IEmailService>();

            _attendanceService = new AttendanceService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockEmailService.Object
            );
        }

        /// <summary>
        /// Helper: setup a mock transaction that auto-commits
        /// </summary>
        private void SetupTransaction()
        {
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockTransaction.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);
            mockTransaction.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockTransaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);
        }

        #region RegisterMemberAsync

        [Fact]
        public async Task RegisterMemberAsync_Success_NoLimit_Registers()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1),
                RegistrationEndDate = DateTime.Now.AddDays(5),
                MaxAttendees = null // no limit
            };
            var user = new User { Email = "a@b.com", FullName = "Test" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Attendances.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            SetupTransaction();

            // Act
            await _attendanceService.RegisterMemberAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.Attendances.AddAsync(
                It.Is<Attendance>(a => a.AttendanceStatus == nameof(AttendanceStatus.REGISTERED))), Times.Once);
        }

        [Fact]
        public async Task RegisterMemberAsync_WithLimit_GotSlot_Registers()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1),
                MaxAttendees = 50,
                RequiresApproval = false
            };
            var user = new User { Email = "a@b.com", FullName = "Test" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Events.TryDecrementSlotAsync(1)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.Attendances.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            SetupTransaction();

            // Act
            await _attendanceService.RegisterMemberAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.Attendances.AddAsync(
                It.Is<Attendance>(a => a.AttendanceStatus == nameof(AttendanceStatus.REGISTERED))), Times.Once);
        }

        [Fact]
        public async Task RegisterMemberAsync_WithLimit_NoSlot_Waitlisted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1),
                MaxAttendees = 50
            };
            var user = new User { Email = "a@b.com", FullName = "Test" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Events.TryDecrementSlotAsync(1)).ReturnsAsync(false); // no slot
            _mockUnitOfWork.Setup(u => u.Attendances.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            SetupTransaction();

            // Act
            await _attendanceService.RegisterMemberAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.Attendances.AddAsync(
                It.Is<Attendance>(a => a.AttendanceStatus == nameof(AttendanceStatus.WAITLIST))), Times.Once);
        }

        [Fact]
        public async Task RegisterMemberAsync_RequiresApproval_PendingStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1),
                MaxAttendees = null,
                RequiresApproval = true
            };
            var user = new User { Email = "a@b.com", FullName = "Test" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Attendances.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            SetupTransaction();

            // Act
            await _attendanceService.RegisterMemberAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.Attendances.AddAsync(
                It.Is<Attendance>(a => a.AttendanceStatus == nameof(AttendanceStatus.PENDING))), Times.Once);
        }

        [Fact]
        public async Task RegisterMemberAsync_EventNotFound_ThrowsNotFoundException()
        {
            var request = new EventRegistrationRequest { EventId = 999, UserId = Guid.NewGuid() };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RegisterMemberAsync_EventEnded_ThrowsDomainException()
        {
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var eventEntity = new Event { EventId = 1, Status = "ENDED" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>();
        }

        [Fact]
        public async Task RegisterMemberAsync_EventOngoing_ThrowsDomainException()
        {
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var eventEntity = new Event { EventId = 1, Status = "ONGOING" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>();
        }

        [Fact]
        public async Task RegisterMemberAsync_RegistrationClosed_ThrowsDomainException()
        {
            var request = new EventRegistrationRequest { EventId = 1, UserId = Guid.NewGuid() };
            var eventEntity = new Event { EventId = 1, Status = "REGISTRATION_CLOSED" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>();
        }

        [Fact]
        public async Task RegisterMemberAsync_AlreadyRegistered_ThrowsConflictException()
        {
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(true);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task RegisterMemberAsync_UserNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            var request = new EventRegistrationRequest { EventId = 1, UserId = userId };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN",
                RegistrationStartDate = DateTime.Now.AddDays(-1)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.IsUserRegisteredAsync(1, userId)).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var act = () => _attendanceService.RegisterMemberAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region GenerateCheckInCodeAsync

        [Fact]
        public async Task GenerateCheckInCodeAsync_Success_ReturnsCode()
        {
            var eventEntity = new Event { EventId = 1 };
            var expectedResponse = new CheckInCodeResponse { EventId = 1, Code = "ABC123" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<CheckInCodeResponse>(eventEntity)).Returns(expectedResponse);

            var result = await _attendanceService.GenerateCheckInCodeAsync(1);

            result.Should().NotBeNull();
            result.EventId.Should().Be(1);
            eventEntity.CheckInCode.Should().NotBeNullOrEmpty();
            eventEntity.CodeExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GenerateCheckInCodeAsync_EventNotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _attendanceService.GenerateCheckInCodeAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region CheckInMemberAsync

        [Fact]
        public async Task CheckInMemberAsync_Success_UpdatesStatus()
        {
            var userId = Guid.NewGuid();
            var request = new CheckInRequest { EventId = 1, UserId = userId, Code = "ABC123" };
            var eventEntity = new Event
            {
                EventId = 1,
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddMinutes(10)
            };
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.REGISTERED)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            await _attendanceService.CheckInMemberAsync(request);

            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.PRESENT));
            attendance.CheckInTime.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckInMemberAsync_EventNotFound_ThrowsNotFoundException()
        {
            var request = new CheckInRequest { EventId = 999, UserId = Guid.NewGuid(), Code = "X" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _attendanceService.CheckInMemberAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CheckInMemberAsync_WrongCode_ThrowsDomainException()
        {
            var request = new CheckInRequest { EventId = 1, UserId = Guid.NewGuid(), Code = "WRONG" };
            var eventEntity = new Event { EventId = 1, CheckInCode = "ABC123", CodeExpiresAt = DateTime.Now.AddMinutes(10) };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.CheckInMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>().WithMessage("*Invalid check-in code*");
        }

        [Fact]
        public async Task CheckInMemberAsync_ExpiredCode_ThrowsDomainException()
        {
            var request = new CheckInRequest { EventId = 1, UserId = Guid.NewGuid(), Code = "ABC123" };
            var eventEntity = new Event { EventId = 1, CheckInCode = "ABC123", CodeExpiresAt = DateTime.Now.AddMinutes(-10) };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.CheckInMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>().WithMessage("*expired*");
        }

        [Fact]
        public async Task CheckInMemberAsync_AttendanceNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            var request = new CheckInRequest { EventId = 1, UserId = userId, Code = "ABC123" };
            var eventEntity = new Event { EventId = 1, CheckInCode = "ABC123", CodeExpiresAt = DateTime.Now.AddMinutes(10) };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync((Attendance?)null);

            var act = () => _attendanceService.CheckInMemberAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region GetMyCheckInQrAsync

        [Fact]
        public async Task GetMyCheckInQrAsync_HasToken_ReturnsResponse()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                CheckInToken = "existing-token"
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);

            var result = await _attendanceService.GetMyCheckInQrAsync(1, userId);

            result.Should().NotBeNull();
            result!.QrContent.Should().Be("existing-token");
        }

        [Fact]
        public async Task GetMyCheckInQrAsync_NoToken_GeneratesToken()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                CheckInToken = null
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _attendanceService.GetMyCheckInQrAsync(1, userId);

            result.Should().NotBeNull();
            result!.QrContent.Should().NotBeNullOrEmpty();
            attendance.CheckInToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetMyCheckInQrAsync_NotRegistered_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId))
                .ReturnsAsync((Attendance?)null);

            var result = await _attendanceService.GetMyCheckInQrAsync(1, userId);
            result.Should().BeNull();
        }

        #endregion

        #region CheckInByQrTokenAsync

        [Fact]
        public async Task CheckInByQrTokenAsync_Success_ReturnsResponse()
        {
            var attendance = new Attendance
            {
                EventId = 1,
                CheckInToken = "token123",
                AttendanceStatus = nameof(AttendanceStatus.REGISTERED),
                User = new User { FullName = "Nguyen Van A" }
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByCheckInTokenAsync("token123")).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _attendanceService.CheckInByQrTokenAsync(1, "token123");

            result.Success.Should().BeTrue();
            result.AlreadyCheckedIn.Should().BeFalse();
            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.PRESENT));
        }

        [Fact]
        public async Task CheckInByQrTokenAsync_AlreadyCheckedIn_ReturnsAlreadyFlag()
        {
            var attendance = new Attendance
            {
                EventId = 1,
                CheckInToken = "token123",
                AttendanceStatus = nameof(AttendanceStatus.PRESENT),
                User = new User { FullName = "Test" }
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByCheckInTokenAsync("token123")).ReturnsAsync(attendance);

            var result = await _attendanceService.CheckInByQrTokenAsync(1, "token123");

            result.AlreadyCheckedIn.Should().BeTrue();
        }

        [Fact]
        public async Task CheckInByQrTokenAsync_EmptyToken_ThrowsDomainException()
        {
            var act = () => _attendanceService.CheckInByQrTokenAsync(1, "  ");
            await act.Should().ThrowAsync<DomainException>();
        }

        [Fact]
        public async Task CheckInByQrTokenAsync_WrongEvent_ThrowsDomainException()
        {
            var attendance = new Attendance { EventId = 2, CheckInToken = "token123" };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByCheckInTokenAsync("token123")).ReturnsAsync(attendance);

            var act = () => _attendanceService.CheckInByQrTokenAsync(1, "token123");
            await act.Should().ThrowAsync<DomainException>();
        }

        #endregion

        #region EvaluateMemberAsync

        [Fact]
        public async Task EvaluateMemberAsync_Success_UpdatesScore()
        {
            var userId = Guid.NewGuid();
            var request = new EvaluateMemberRequest { EventId = 1, UserId = userId, Score = 85, Comment = "Good" };
            var eventEntity = new Event { EventId = 1, EndDate = DateTime.Now.AddDays(-1) };
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.PRESENT)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            await _attendanceService.EvaluateMemberAsync(request);

            attendance.Score.Should().Be(85);
            attendance.Comment.Should().Be("Good");
        }

        [Fact]
        public async Task EvaluateMemberAsync_EventNotFound_ThrowsNotFoundException()
        {
            var request = new EvaluateMemberRequest { EventId = 999, UserId = Guid.NewGuid(), Score = 80 };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _attendanceService.EvaluateMemberAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task EvaluateMemberAsync_EventNotEnded_ThrowsDomainException()
        {
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 80 };
            var eventEntity = new Event { EventId = 1, EndDate = DateTime.Now.AddDays(5) };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _attendanceService.EvaluateMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>().WithMessage("*Cannot evaluate*before*ended*");
        }

        [Fact]
        public async Task EvaluateMemberAsync_NotPresent_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var request = new EvaluateMemberRequest { EventId = 1, UserId = userId, Score = 80 };
            var eventEntity = new Event { EventId = 1, EndDate = DateTime.Now.AddDays(-1) };
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.ABSENT)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);

            var act = () => _attendanceService.EvaluateMemberAsync(request);
            await act.Should().ThrowAsync<DomainException>().WithMessage("*Cannot evaluate*");
        }

        #endregion

        #region GetEventAttendeesAsync

        [Fact]
        public async Task GetEventAttendeesAsync_Success_ReturnsList()
        {
            var attendances = new List<Attendance> { new Attendance { EventId = 1 } };
            var dtos = new List<AttendanceDetailDto> { new AttendanceDetailDto { EventId = 1 } };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(new Event { EventId = 1 });
            _mockUnitOfWork.Setup(u => u.Attendances.GetAttendeesByEventAsync(1)).ReturnsAsync(attendances);
            _mockMapper.Setup(m => m.Map<IEnumerable<AttendanceDetailDto>>(attendances)).Returns(dtos);

            var result = await _attendanceService.GetEventAttendeesAsync(1);
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetEventAttendeesAsync_EventNotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _attendanceService.GetEventAttendeesAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region ApproveRegistrationAsync

        [Fact]
        public async Task ApproveRegistrationAsync_Success_SetsRegistered()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.PENDING),
                User = new User { Email = "a@b.com", FullName = "Test" },
                Event = new Event { EventName = "E" }
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            await _attendanceService.ApproveRegistrationAsync(1, userId);

            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.REGISTERED));
        }

        [Fact]
        public async Task ApproveRegistrationAsync_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync((Attendance?)null);

            var act = () => _attendanceService.ApproveRegistrationAsync(1, userId);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ApproveRegistrationAsync_WrongStatus_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                AttendanceStatus = nameof(AttendanceStatus.PRESENT)
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);

            var act = () => _attendanceService.ApproveRegistrationAsync(1, userId);
            await act.Should().ThrowAsync<DomainException>();
        }

        #endregion

        #region RejectRegistrationAsync

        [Fact]
        public async Task RejectRegistrationAsync_Success_SetsRejected()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.PENDING)
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(new Event { EventId = 1, MaxAttendees = null });

            await _attendanceService.RejectRegistrationAsync(1, userId);

            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.REJECTED));
        }

        [Fact]
        public async Task RejectRegistrationAsync_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync((Attendance?)null);

            var act = () => _attendanceService.RejectRegistrationAsync(1, userId);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region BulkApproveAsync

        [Fact]
        public async Task BulkApproveAsync_ApprovesMultiple_ReturnsCount()
        {
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var att1 = new Attendance { AttendanceStatus = nameof(AttendanceStatus.PENDING) };
            var att2 = new Attendance { AttendanceStatus = nameof(AttendanceStatus.WAITLIST) };

            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, user1)).ReturnsAsync(att1);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, user2)).ReturnsAsync(att2);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _attendanceService.BulkApproveAsync(1, new List<Guid> { user1, user2 });

            result.Should().Be(2);
            att1.AttendanceStatus.Should().Be(nameof(AttendanceStatus.REGISTERED));
            att2.AttendanceStatus.Should().Be(nameof(AttendanceStatus.REGISTERED));
        }

        [Fact]
        public async Task BulkApproveAsync_SkipsNonExistent_ReturnsZero()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync((Attendance?)null);

            var result = await _attendanceService.BulkApproveAsync(1, new List<Guid> { userId });
            result.Should().Be(0);
        }

        #endregion

        #region CancelRegistrationAsync

        [Fact]
        public async Task CancelRegistrationAsync_Success_SetsCancelled()
        {
            var userId = Guid.NewGuid();
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.REGISTERED)
            };
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(new Event { EventId = 1, MaxAttendees = null });

            await _attendanceService.CancelRegistrationAsync(1, userId);

            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.CANCELLED));
        }

        [Fact]
        public async Task CancelRegistrationAsync_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync((Attendance?)null);

            var act = () => _attendanceService.CancelRegistrationAsync(1, userId);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion
    }
}
