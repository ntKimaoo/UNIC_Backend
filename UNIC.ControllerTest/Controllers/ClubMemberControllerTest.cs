//using BusinessLogic.DTOs;
//using BusinessLogic.Services.Interface;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using Presentation.Controllers;
//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using Xunit;

//namespace UNIC.ControllerTest.Controllers
//{
//    public class ClubMemberControllerTest
//    {
//        private readonly Mock<IClubMemberService> _mockService;
//        private readonly Mock<IPolicyService> _mockPolicyService;
//        private readonly ClubMemberController _controller;

//        private static readonly Guid _userId = Guid.NewGuid();

//        public ClubMemberControllerTest()
//        {
//            _mockService = new Mock<IClubMemberService>();
//            _mockPolicyService = new Mock<IPolicyService>();
//            _controller = new ClubMemberController(_mockService.Object, _mockPolicyService.Object);

//            // Set up a default authenticated user
//            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
//            var identity = new ClaimsIdentity(claims, "Test");
//            var principal = new ClaimsPrincipal(identity);
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext { User = principal }
//            };
//        }

//        private static ClubMemberResponseDto CreateMemberDto(int id = 1, int clubId = 1) => new()
//        {
//            ClubMemberId = id,
//            UserId = _userId,
//            FullName = "Test User",
//            Email = "test@example.com",
//            ClubId = clubId,
//            ClubRoleId = 10,
//            RoleName = "Member",
//            JoinDate = DateTime.UtcNow,
//            Status = "ACTIVE"
//        };

//        #region GetMembers

//        [Fact]
//        public async Task GetMembers_WithPagination_ReturnsPagedResult()
//        {
//            var paged = new PagedResultDto<ClubMemberResponseDto>
//            {
//                Items = new List<ClubMemberResponseDto> { CreateMemberDto() },
//                TotalCount = 1, PageNumber = 1, PageSize = 10, TotalPages = 1
//            };
//            _mockService.Setup(s => s.GetMembersByClubAsync(1, 10, 1, null, null, null)).ReturnsAsync(paged);

//            var result = await _controller.GetMembers(1, 10, 1, null, null, null);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal(200, ok.StatusCode);
//        }

//        [Fact]
//        public async Task GetMembers_WithoutPagination_ReturnsAll()
//        {
//            var paged = new PagedResultDto<ClubMemberResponseDto>
//            {
//                Items = new List<ClubMemberResponseDto> { CreateMemberDto() },
//                TotalCount = 1, PageNumber = 1, PageSize = 0, TotalPages = 1
//            };
//            _mockService.Setup(s => s.GetMembersByClubAsync(1, null, null, null, null, null)).ReturnsAsync(paged);

//            var result = await _controller.GetMembers(1, null, null, null, null, null);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            var value = ok.Value!;
//            var type = value.GetType();
//            Assert.True((bool)type.GetProperty("success")!.GetValue(value)!);
//        }

//        #endregion

//        #region GetMember

//        [Fact]
//        public async Task GetMember_ReturnsOk_WhenFound()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());

//            var result = await _controller.GetMember(1, 1);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetMember_ReturnsNotFound_WhenNull()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var result = await _controller.GetMember(1, 99);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetMember_ReturnsNotFound_WhenClubMismatch()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto(1, 2));

//            var result = await _controller.GetMember(1, 1); // clubId=1, but member belongs to clubId=2

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        #endregion

//        #region AddMember

//        [Fact]
//        public async Task AddMember_ReturnsCreated_WhenSuccess()
//        {
//            var member = CreateMemberDto();
//            var dto = new AddUserToClubDto { UserId = _userId, ClubRoleId = 10 };

//            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, _userId)).ReturnsAsync(member);

//            var result = await _controller.AddMember(1, dto);

//            var created = Assert.IsType<CreatedAtActionResult>(result);
//            Assert.Equal(201, created.StatusCode);
//        }

//        [Fact]
//        public async Task AddMember_ReturnsBadRequest_WhenModelInvalid()
//        {
//            _controller.ModelState.AddModelError("UserId", "Required");

//            var result = await _controller.AddMember(1, new AddUserToClubDto());

//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task AddMember_ReturnsNotFound_WhenKeyNotFound()
//        {
//            var dto = new AddUserToClubDto { UserId = _userId };
//            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, _userId))
//                        .ThrowsAsync(new KeyNotFoundException("Club not found"));

//            var result = await _controller.AddMember(1, dto);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task AddMember_ReturnsConflict_WhenAlreadyMember()
//        {
//            var dto = new AddUserToClubDto { UserId = _userId };
//            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, _userId))
//                        .ThrowsAsync(new InvalidOperationException("already a member"));

//            var result = await _controller.AddMember(1, dto);

//            var conflict = Assert.IsType<ConflictObjectResult>(result);
//            Assert.Equal(409, conflict.StatusCode);
//        }

//        [Fact]
//        public async Task AddMember_Returns500_WhenUnexpectedError()
//        {
//            var dto = new AddUserToClubDto { UserId = _userId };
//            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, _userId))
//                        .ThrowsAsync(new Exception("DB error"));

//            var result = await _controller.AddMember(1, dto);

//            var obj = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, obj.StatusCode);
//        }

//        #endregion

//        #region UpdateMemberRole

//        [Fact]
//        public async Task UpdateMemberRole_ReturnsOk_WhenSuccess()
//        {
//            var member = CreateMemberDto();
//            var dto = new UpdateMemberRoleDto { ClubRoleId = 20 };
//            _mockService.Setup(s => s.UpdateMemberRoleAsync(1, dto)).ReturnsAsync(member);

//            var result = await _controller.UpdateMemberRole(1, 1, dto);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateMemberRole_ReturnsBadRequest_WhenModelInvalid()
//        {
//            _controller.ModelState.AddModelError("ClubRoleId", "Required");

//            var result = await _controller.UpdateMemberRole(1, 1, new UpdateMemberRoleDto());

//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateMemberRole_ReturnsNotFound_WhenNull()
//        {
//            var dto = new UpdateMemberRoleDto { ClubRoleId = 20 };
//            _mockService.Setup(s => s.UpdateMemberRoleAsync(99, dto)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var result = await _controller.UpdateMemberRole(1, 99, dto);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateMemberRole_ReturnsNotFound_WhenClubMismatch()
//        {
//            var dto = new UpdateMemberRoleDto { ClubRoleId = 20 };
//            _mockService.Setup(s => s.UpdateMemberRoleAsync(1, dto))
//                        .ReturnsAsync(CreateMemberDto(1, 2)); // member belongs to club 2

//            var result = await _controller.UpdateMemberRole(1, 1, dto);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateMemberRole_Returns500_WhenException()
//        {
//            var dto = new UpdateMemberRoleDto { ClubRoleId = 20 };
//            _mockService.Setup(s => s.UpdateMemberRoleAsync(1, dto))
//                        .ThrowsAsync(new Exception("DB error"));

//            var result = await _controller.UpdateMemberRole(1, 1, dto);

//            var obj = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, obj.StatusCode);
//        }

//        #endregion

//        #region RemoveMember

//        [Fact]
//        public async Task RemoveMember_ReturnsOk_WhenSuccess()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockService.Setup(s => s.RemoveMemberAsync(1)).ReturnsAsync(true);

//            var result = await _controller.RemoveMember(1, 1);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task RemoveMember_ReturnsNotFound_WhenMemberNull()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var result = await _controller.RemoveMember(1, 99);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task RemoveMember_ReturnsNotFound_WhenClubMismatch()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto(1, 2));

//            var result = await _controller.RemoveMember(1, 1);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task RemoveMember_Returns500_WhenRemoveFails()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockService.Setup(s => s.RemoveMemberAsync(1)).ReturnsAsync(false);

//            var result = await _controller.RemoveMember(1, 1);

//            var obj = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, obj.StatusCode);
//        }

//        #endregion

//        #region GetClubsByUser

//        [Fact]
//        public async Task GetClubsByUser_ReturnsOk()
//        {
//            _mockService.Setup(s => s.GetMyClubsAsync(_userId))
//                        .ReturnsAsync(new List<ClubMemberResponseDto> { CreateMemberDto() });

//            var result = await _controller.GetClubsByUser(_userId);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetClubsByUser_ReturnsBadRequest_WhenEmptyGuid()
//        {
//            var result = await _controller.GetClubsByUser(Guid.Empty);

//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        #endregion

//        #region GetMemberPolicies

//        [Fact]
//        public async Task GetMemberPolicies_ReturnsOk_WhenFound()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockPolicyService.Setup(s => s.GetUserDirectPoliciesAsync(_userId))
//                              .ReturnsAsync(new List<PolicyResponseDto>());

//            var result = await _controller.GetMemberPolicies(1, 1);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetMemberPolicies_ReturnsNotFound_WhenMemberNull()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var result = await _controller.GetMemberPolicies(1, 99);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetMemberPolicies_ReturnsNotFound_WhenClubMismatch()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto(1, 2));

//            var result = await _controller.GetMemberPolicies(1, 1);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        #endregion

//        #region AssignPolicies

//        [Fact]
//        public async Task AssignPolicies_ReturnsOk_WhenSuccess()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockPolicyService.Setup(s => s.AssignPoliciesToUserAsync(_userId, It.IsAny<IEnumerable<int>>()))
//                              .Returns(Task.CompletedTask);
//            _mockPolicyService.Setup(s => s.GetUserDirectPoliciesAsync(_userId))
//                              .ReturnsAsync(new List<PolicyResponseDto>());

//            var dto = new MemberPolicyDto { PolicyIds = new List<int> { 1, 2 } };
//            var result = await _controller.AssignPolicies(1, 1, dto);

//            Assert.IsType<OkObjectResult>(result);
//            _mockPolicyService.Verify(s => s.AssignPoliciesToUserAsync(_userId, dto.PolicyIds), Times.Once);
//        }

//        [Fact]
//        public async Task AssignPolicies_ReturnsBadRequest_WhenModelInvalid()
//        {
//            _controller.ModelState.AddModelError("PolicyIds", "Required");

//            var result = await _controller.AssignPolicies(1, 1, new MemberPolicyDto());

//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task AssignPolicies_ReturnsNotFound_WhenMemberNotFound()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var dto = new MemberPolicyDto { PolicyIds = new List<int> { 1 } };
//            var result = await _controller.AssignPolicies(1, 99, dto);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        #endregion

//        #region SetPolicies

//        [Fact]
//        public async Task SetPolicies_ReturnsOk_WhenSuccess()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockPolicyService.Setup(s => s.SetUserPoliciesAsync(_userId, It.IsAny<IEnumerable<int>>()))
//                              .Returns(Task.CompletedTask);
//            _mockPolicyService.Setup(s => s.GetUserDirectPoliciesAsync(_userId))
//                              .ReturnsAsync(new List<PolicyResponseDto>());

//            var dto = new MemberPolicyDto { PolicyIds = new List<int> { 1, 2 } };
//            var result = await _controller.SetPolicies(1, 1, dto);

//            Assert.IsType<OkObjectResult>(result);
//            _mockPolicyService.Verify(s => s.SetUserPoliciesAsync(_userId, dto.PolicyIds), Times.Once);
//        }

//        [Fact]
//        public async Task SetPolicies_ReturnsBadRequest_WhenModelInvalid()
//        {
//            _controller.ModelState.AddModelError("PolicyIds", "Required");

//            var result = await _controller.SetPolicies(1, 1, new MemberPolicyDto());

//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task SetPolicies_ReturnsNotFound_WhenMemberNotFound()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var dto = new MemberPolicyDto { PolicyIds = new List<int> { 1 } };
//            var result = await _controller.SetPolicies(1, 99, dto);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        #endregion

//        #region RevokePolicy

//        [Fact]
//        public async Task RevokePolicy_ReturnsOk_WhenSuccess()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockPolicyService.Setup(s => s.RevokePolicyFromUserAsync(_userId, 5)).ReturnsAsync(true);

//            var result = await _controller.RevokePolicy(1, 1, 5);

//            Assert.IsType<OkObjectResult>(result);
//        }

//        [Fact]
//        public async Task RevokePolicy_ReturnsNotFound_WhenMemberNotFound()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(99)).ReturnsAsync((ClubMemberResponseDto?)null);

//            var result = await _controller.RevokePolicy(1, 99, 5);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task RevokePolicy_ReturnsNotFound_WhenPolicyNotAssigned()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto());
//            _mockPolicyService.Setup(s => s.RevokePolicyFromUserAsync(_userId, 5)).ReturnsAsync(false);

//            var result = await _controller.RevokePolicy(1, 1, 5);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task RevokePolicy_ReturnsNotFound_WhenClubMismatch()
//        {
//            _mockService.Setup(s => s.GetMemberByIdAsync(1)).ReturnsAsync(CreateMemberDto(1, 2));

//            var result = await _controller.RevokePolicy(1, 1, 5);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        #endregion
//    }
//}
