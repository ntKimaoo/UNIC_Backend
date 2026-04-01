using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UNIC.BusinessLogic.DTOs;
using UNIC.Presentation.Controllers;

namespace UNIC.ServiceTest.Controllers
{

    public class ClubCreationRequestControllerTest
    {
        private readonly Mock<IClubCreationRequestService> _serviceMock = new();
        private readonly ClubCreationRequestController _sut;

        private static T? Prop<T>(object? obj, string name) =>
            (T?)obj?.GetType().GetProperty(name)?.GetValue(obj);

        public ClubCreationRequestControllerTest()
        {
            _sut = new ClubCreationRequestController(_serviceMock.Object);
        }

        private static ClubCreationRequestDto BuildDto(int id = 1) => new()
        {
            RequestId = id,
            UserId = Guid.NewGuid(),
            ClubName = "Club",
            Description = "Desc",
            Reason = "Reason",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task GetAll_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetAllAsync())
                        .ReturnsAsync(new List<ClubCreationRequestDto> { BuildDto(1), BuildDto(2) });

            var result = await _sut.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(BuildDto(1));

            var result = await _sut.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ClubCreationRequestDto>(ok.Value, "data")!.RequestId);
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ClubCreationRequestDto?)null);

            var result = await _sut.GetById(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetByUserId_WhenCalled_Returns200WithList()
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByUserIdAsync(userId))
                        .ReturnsAsync(new List<ClubCreationRequestDto> { BuildDto() });

            var result = await _sut.GetByUserId(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HasPending_WhenCalled_Returns200WithBool(bool hasPending)
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.HasPendingRequestAsync(userId)).ReturnsAsync(hasPending);

            var result = await _sut.HasPending(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(hasPending, Prop<bool>(ok.Value, "data"));
        }

        [Fact]
        public async Task Create_WhenCalled_Returns200()
        {
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateClubCreationRequestDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.Create(new CreateClubCreationRequestDto
            {
                UserId = Guid.NewGuid(),
                ClubName = "Club",
                Description = "D",
                Reason = "R"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task Update_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateClubCreationRequestDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.Update(1, new UpdateClubCreationRequestDto());

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateClubCreationRequestDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.Update(999, new UpdateClubCreationRequestDto());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_WhenFound_Returns200WithSuccessMessage()
        {
            _serviceMock.Setup(s => s.UpdateStatusAsync(1, It.IsAny<UpdateClubRequestStatusDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.UpdateStatus(1, new UpdateClubRequestStatusDto { Status = "Approved" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateStatus_WhenNotFound_Returns404WithMessage()
        {
            _serviceMock.Setup(s => s.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<UpdateClubRequestStatusDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.UpdateStatus(999, new UpdateClubRequestStatusDto { Status = "Approved" });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.False(Prop<bool>(notFound.Value, "success"));
        }

        [Fact]
        public async Task Delete_WhenCalled_Returns200()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.Delete(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }
    }
}