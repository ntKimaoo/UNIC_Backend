using BusinessLogic.DTOs;
using BusinessLogic.Services;
using DataAccess.Repositories.Interface;
using Moq;
using UNIC.BusinessLogic.DTOs;
using UNIC.DataAccess.Models;

namespace UNIC.ServiceTest.Services
{

    public class ClubCreationRequestServiceTest
    {
        private readonly Mock<IClubCreationRequestRepository> _repoMock = new();
        private readonly ClubCreationRequestService _sut;

        public ClubCreationRequestServiceTest()
        {
            _sut = new ClubCreationRequestService(_repoMock.Object);
        }

        private static ClubCreationRequest BuildRequest(int id = 1, Guid? userId = null) => new()
        {
            RequestId = id,
            UserId = userId ?? Guid.NewGuid(),
            ClubName = "Chess Club",
            Description = "For chess lovers",
            Reason = "Need a club",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task GetAllAsync_WhenRequestsExist_ReturnsMappedDtos()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<ClubCreationRequest> { BuildRequest(1), BuildRequest(2) });

            var result = (await _sut.GetAllAsync()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].RequestId);
            Assert.Equal("Chess Club", result[0].ClubName);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildRequest(5));

            var result = await _sut.GetByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal(5, result.RequestId);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((ClubCreationRequest?)null);

            var result = await _sut.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenCalled_ReturnsDtosForThatUser()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                     .ReturnsAsync(new List<ClubCreationRequest> { BuildRequest(1, userId), BuildRequest(2, userId) });

            var result = (await _sut.GetByUserIdAsync(userId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.UserId));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HasPendingRequestAsync_ReturnsSameValueAsRepository(bool hasPending)
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.HasPendingRequestAsync(userId)).ReturnsAsync(hasPending);

            var result = await _sut.HasPendingRequestAsync(userId);

            Assert.Equal(hasPending, result);
        }

        [Fact]
        public async Task CreateAsync_WhenCalled_CreatesRequestWithPendingStatusAndReturnsTrue()
        {
            ClubCreationRequest? captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<ClubCreationRequest>()))
                     .Callback<ClubCreationRequest>(r => captured = r)
                     .Returns(Task.CompletedTask);

            var result = await _sut.CreateAsync(new CreateClubCreationRequestDto
            {
                UserId = Guid.NewGuid(),
                ClubName = "Art Club",
                Description = "Art",
                Reason = "Reason"
            });

            Assert.True(result);
            Assert.NotNull(captured);
            Assert.Equal("Pending", captured!.Status);
            Assert.Equal("Art Club", captured.ClubName);
        }

        [Fact]
        public async Task UpdateAsync_WhenRequestNotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((ClubCreationRequest?)null);

            var result = await _sut.UpdateAsync(1, new UpdateClubCreationRequestDto());

            Assert.False(result);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<ClubCreationRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenRequestFound_UpdatesFieldsAndReturnsTrue()
        {
            var request = BuildRequest(1);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _repoMock.Setup(r => r.UpdateAsync(request)).Returns(Task.CompletedTask);

            var result = await _sut.UpdateAsync(1, new UpdateClubCreationRequestDto
            {
                ClubName = "New Name",
                Description = "New Desc",
                Reason = "New Reason",
                Status = "Approved"
            });

            Assert.True(result);
            Assert.Equal("New Name", request.ClubName);
            Assert.Equal("Approved", request.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenFound_UpdatesStatusAdminCommentAndReviewedAt()
        {
            var request = BuildRequest(1);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _repoMock.Setup(r => r.UpdateAsync(request)).Returns(Task.CompletedTask);

            var result = await _sut.UpdateStatusAsync(1,
                new UpdateClubRequestStatusDto { Status = "Rejected", AdminComment = "Not eligible" });

            Assert.True(result);
            Assert.Equal("Rejected", request.Status);
            Assert.Equal("Not eligible", request.AdminComment);
            Assert.NotNull(request.ReviewedAt);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenNotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((ClubCreationRequest?)null);

            var result = await _sut.UpdateStatusAsync(99,
                new UpdateClubRequestStatusDto { Status = "Approved" });

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_WhenCalled_DelegatesToRepositoryAndReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteAsync(3)).Returns(Task.CompletedTask);

            var result = await _sut.DeleteAsync(3);

            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(3), Times.Once);
        }
    }
}