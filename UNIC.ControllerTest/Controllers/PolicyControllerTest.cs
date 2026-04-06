using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;
using UNIC.DataAccess.Models;
namespace UNIC.ControllerTest.Controllers
{
    public class PolicyControllerTest
    {
        private readonly Mock<IPolicyService> _mockPolicyService;
        private readonly PolicyController _controller;

        public PolicyControllerTest()
        {
            _mockPolicyService = new Mock<IPolicyService>();
            _controller = new PolicyController(_mockPolicyService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk_WithListOfPolicies()
        {
            // Arrange
            var policies = new List<PolicyGroup>
        {
            new PolicyGroup { PolicyGroupId = 1, Name = "Policy A" },
            new PolicyGroup { PolicyGroupId = 2, Name = "Policy B" }
        };

            _mockPolicyService.Setup(s => s.GetAllPolicyGroupAsync())
                              .ReturnsAsync(policies);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion
        #region GetAllGroupById

        [Fact]
        public async Task GetAllGroupById_ReturnsOk_WithPolicies()
        {
            // Arrange
            int groupId = 1;

            var policies = new List<Policy>
    {
        new Policy { Id = 1, Name = "Policy 1" },
        new Policy { Id = 2, Name = "Policy 2" }
    };

            _mockPolicyService.Setup(s => s.GetAllPoliciesByGroupAsync(groupId))
                              .ReturnsAsync(policies);

            // Act
            var result = await _controller.GetAllGroupById(groupId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal(2, root.GetProperty("data").GetArrayLength());

            _mockPolicyService.Verify(s => s.GetAllPoliciesByGroupAsync(groupId), Times.Once);
        }

        #endregion
        #region GetUserPolicies

        [Fact]
        public async Task GetUserPolicies_ReturnsOk_WithPolicies()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var policies = new List<Policy>
    {
        new Policy { Id = 1, Name = "Policy 1" },
        new Policy { Id = 2, Name = "Policy 2" }
    };

            _mockPolicyService.Setup(s => s.GetUserPoliciesAsync(userId))
                              .ReturnsAsync(policies);

            // Act
            var result = await _controller.GetUserPolicies(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal(2, root.GetProperty("data").GetArrayLength());

            _mockPolicyService.Verify(s => s.GetUserPoliciesAsync(userId), Times.Once);
        }

        #endregion
        #region GetUserDirectPolicies

        [Fact]
        public async Task GetUserDirectPolicies_ReturnsOk_WithPolicies()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var policies = new List<PolicyResponseDto>
    {
        new PolicyResponseDto { Id = 1, Title = "Direct Policy 1" },
        new PolicyResponseDto { Id = 2, Title = "Direct Policy 2" }
    };

            _mockPolicyService.Setup(s => s.GetUserDirectPoliciesAsync(userId))
                              .ReturnsAsync(policies);

            // Act
            var result = await _controller.GetUserDirectPolicies(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal(2, root.GetProperty("data").GetArrayLength());

            _mockPolicyService.Verify(s => s.GetUserDirectPoliciesAsync(userId), Times.Once);
        }


        #endregion
        #region HasPolicy

        [Fact]
        public async Task HasPolicy_ReturnsOk_WhenUserHasPolicy()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyTitle = "Policy A";

            _mockPolicyService.Setup(s => s.HasUserPolicyAsync(userId, policyTitle))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.HasPolicy(userId, policyTitle);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.True(root.GetProperty("data").GetBoolean());

            _mockPolicyService.Verify(s => s.HasUserPolicyAsync(userId, policyTitle), Times.Once);
        }

        #endregion
        #region AssignPolicies

        [Fact]
        public async Task AssignPolicies_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyIds = new List<int> { 1, 2, 3 };

            _mockPolicyService.Setup(s => s.AssignPoliciesToUserAsync(userId, policyIds))
                              .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AssignPolicies(userId, policyIds);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal("Policies assigned successfully", root.GetProperty("message").GetString());

            _mockPolicyService.Verify(s => s.AssignPoliciesToUserAsync(userId, policyIds), Times.Once);
        }
        #endregion
        #region RevokePolicy

        [Fact]
        public async Task RevokePolicy_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            int policyId = 1;

            _mockPolicyService.Setup(s => s.RevokePolicyFromUserAsync(userId, policyId))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokePolicy(userId, policyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal("Policy revoked successfully", root.GetProperty("message").GetString());

            _mockPolicyService.Verify(s => s.RevokePolicyFromUserAsync(userId, policyId), Times.Once);
        }

        [Fact]
        public async Task RevokePolicy_ReturnsNotFound_WhenPolicyDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            int policyId = 99;

            _mockPolicyService.Setup(s => s.RevokePolicyFromUserAsync(userId, policyId))
                              .ReturnsAsync(false);

            // Act
            var result = await _controller.RevokePolicy(userId, policyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(notFoundResult.Value));
            var root = json.RootElement;

            Assert.False(root.GetProperty("success").GetBoolean());
            Assert.Equal("Policy assignment not found", root.GetProperty("message").GetString());

            _mockPolicyService.Verify(s => s.RevokePolicyFromUserAsync(userId, policyId), Times.Once);
        }

        #endregion
        #region SetUserPolicies

        [Fact]
        public async Task SetUserPolicies_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyIds = new List<int> { 1, 2, 3 };

            _mockPolicyService.Setup(s => s.SetUserPoliciesAsync(userId, policyIds))
                              .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SetUserPolicies(userId, policyIds);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal("User policies updated successfully", root.GetProperty("message").GetString());

            _mockPolicyService.Verify(s => s.SetUserPoliciesAsync(userId, policyIds), Times.Once);
        }

        #endregion
    }
}
