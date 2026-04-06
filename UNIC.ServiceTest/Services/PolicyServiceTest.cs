using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class PolicyServiceTest
    {
        private readonly Mock<IPolicyRepository> _mockPolicyRepository;
        private readonly PolicyService _policyService;

        public PolicyServiceTest()
        {
            _mockPolicyRepository = new Mock<IPolicyRepository>();
            _policyService = new PolicyService(_mockPolicyRepository.Object);
        }

        [Fact]
        public async Task GetUserPoliciesAsync_ShouldReturnPolicies_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedPolicies = new List<Policy> { new Policy { Id = 1, Title = "Policy1" } };
            _mockPolicyRepository.Setup(r => r.GetUserPoliciesAsync(userId))
                .ReturnsAsync(expectedPolicies);

            // Act
            var result = await _policyService.GetUserPoliciesAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedPolicies);
            _mockPolicyRepository.Verify(r => r.GetUserPoliciesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task HasUserPolicyAsync_ShouldReturnTrue_WhenUserHasPolicy()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyTitle = "Policy1";
            _mockPolicyRepository.Setup(r => r.HasUserPolicyAsync(userId, policyTitle))
                .ReturnsAsync(true);

            // Act
            var result = await _policyService.HasUserPolicyAsync(userId, policyTitle);

            // Assert
            result.Should().BeTrue();
            _mockPolicyRepository.Verify(r => r.HasUserPolicyAsync(userId, policyTitle), Times.Once);
        }

        [Fact]
        public async Task GetAllPolicyGroupAsync_ShouldReturnPolicyGroups_WhenCalled()
        {
            // Arrange
            var expectedGroups = new List<PolicyGroup> { new PolicyGroup { PolicyGroupId = 1, Name = "Group1" } };
            _mockPolicyRepository.Setup(r => r.GetAllPolicyGroupAsync())
                .ReturnsAsync(expectedGroups);

            // Act
            var result = await _policyService.GetAllPolicyGroupAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedGroups);
            _mockPolicyRepository.Verify(r => r.GetAllPolicyGroupAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllPoliciesByGroupAsync_ShouldReturnPolicies_WhenCalled()
        {
            // Arrange
            var groupId = 1;
            var expectedPolicies = new List<Policy> { new Policy { Id = 1, Title = "Policy1" } };
            _mockPolicyRepository.Setup(r => r.GetAllPoliciesByGroupAsync(groupId))
                .ReturnsAsync(expectedPolicies);

            // Act
            var result = await _policyService.GetAllPoliciesByGroupAsync(groupId);

            // Assert
            result.Should().BeEquivalentTo(expectedPolicies);
            _mockPolicyRepository.Verify(r => r.GetAllPoliciesByGroupAsync(groupId), Times.Once);
        }

        [Fact]
        public async Task GetUserDirectPoliciesAsync_ShouldReturnMappedDtos_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policiesFromRepo = new List<Policy>
            {
                new Policy { Id = 1, Title = "Policy1", Description = "Desc1" },
                new Policy { Id = 2, Title = "Policy2", Description = "Desc2" }
            };

            _mockPolicyRepository.Setup(r => r.GetUserDirectPoliciesAsync(userId))
                .ReturnsAsync(policiesFromRepo);

            // Act
            var result = await _policyService.GetUserDirectPoliciesAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var resultList = result.ToList();
            resultList[0].Id.Should().Be(1);
            resultList[0].Title.Should().Be("Policy1");
            resultList[0].Description.Should().Be("Desc1");

            resultList[1].Id.Should().Be(2);
            resultList[1].Title.Should().Be("Policy2");
            resultList[1].Description.Should().Be("Desc2");

            _mockPolicyRepository.Verify(r => r.GetUserDirectPoliciesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AssignPoliciesToUserAsync_ShouldCallRepository_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyIds = new List<int> { 1, 2 };

            _mockPolicyRepository.Setup(r => r.AssignPoliciesToUserAsync(userId, policyIds))
                .Returns(Task.CompletedTask);

            // Act
            await _policyService.AssignPoliciesToUserAsync(userId, policyIds);

            // Assert
            _mockPolicyRepository.Verify(r => r.AssignPoliciesToUserAsync(userId, policyIds), Times.Once);
        }

        [Fact]
        public async Task RevokePolicyFromUserAsync_ShouldReturnStatus_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyId = 1;

            _mockPolicyRepository.Setup(r => r.RevokePolicyFromUserAsync(userId, policyId))
                .ReturnsAsync(true);

            // Act
            var result = await _policyService.RevokePolicyFromUserAsync(userId, policyId);

            // Assert
            result.Should().BeTrue();
            _mockPolicyRepository.Verify(r => r.RevokePolicyFromUserAsync(userId, policyId), Times.Once);
        }

        [Fact]
        public async Task SetUserPoliciesAsync_ShouldCallRepository_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policyIds = new List<int> { 1, 2, 3 };

            _mockPolicyRepository.Setup(r => r.SetUserPoliciesAsync(userId, policyIds))
                .Returns(Task.CompletedTask);

            // Act
            await _policyService.SetUserPoliciesAsync(userId, policyIds);

            // Assert
            _mockPolicyRepository.Verify(r => r.SetUserPoliciesAsync(userId, policyIds), Times.Once);
        }

        [Fact]
        public async Task HasMemberPolicyInClubAsync_ShouldReturnExpectedResult_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var clubId = 1;
            var policyTitle = "Policy1";

            _mockPolicyRepository.Setup(r => r.HasMemberPolicyInClubAsync(userId, clubId, policyTitle))
                .ReturnsAsync(true);

            // Act
            var result = await _policyService.HasMemberPolicyInClubAsync(userId, clubId, policyTitle);

            // Assert
            result.Should().BeTrue();
            _mockPolicyRepository.Verify(r => r.HasMemberPolicyInClubAsync(userId, clubId, policyTitle), Times.Once);
        }
    }
}
