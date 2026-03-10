using BusinessLogic.Services.Implementation;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class PolicyServiceTest
    {
        private readonly Mock<IPolicyRepository> _mockRepo;
        private readonly PolicyService _policyService;

        public PolicyServiceTest()
        {
            _mockRepo = new Mock<IPolicyRepository>();
            _policyService = new PolicyService(_mockRepo.Object);
        }

        [Fact]
        public async Task GetUserPoliciesAsync_ShouldReturnPolicies()
        {
            var userId = Guid.NewGuid();
            var policies = new List<Policy> { new Policy() };
            _mockRepo.Setup(r => r.GetUserPoliciesAsync(userId)).ReturnsAsync(policies);

            var result = await _policyService.GetUserPoliciesAsync(userId);
            Assert.Single(result);
            _mockRepo.Verify(r => r.GetUserPoliciesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task HasUserPolicyAsync_ShouldReturnRepoResult()
        {
            var userId = Guid.NewGuid();
            _mockRepo.Setup(r => r.HasUserPolicyAsync(userId, "Policy1")).ReturnsAsync(true);

            var result = await _policyService.HasUserPolicyAsync(userId, "Policy1");
            Assert.True(result);
            _mockRepo.Verify(r => r.HasUserPolicyAsync(userId, "Policy1"), Times.Once);
        }

        [Fact]
        public async Task GetAllPolicyGroupAsync_ShouldReturnGroups()
        {
            var groups = new List<PolicyGroup> { new PolicyGroup() };
            _mockRepo.Setup(r => r.GetAllPolicyGroupAsync()).ReturnsAsync(groups);

            var result = await _policyService.GetAllPolicyGroupAsync();
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllPoliciesByGroupAsync_ShouldReturnPolicies()
        {
            var policies = new List<Policy> { new Policy() };
            _mockRepo.Setup(r => r.GetAllPoliciesByGroupAsync(1)).ReturnsAsync(policies);

            var result = await _policyService.GetAllPoliciesByGroupAsync(1);
            Assert.Single(result);
        }
    }
}
