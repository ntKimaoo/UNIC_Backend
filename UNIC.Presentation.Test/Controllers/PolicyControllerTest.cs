using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class PolicyControllerTest
    {
        private readonly Mock<IPolicyService> _mockService;
        private readonly PolicyController _controller;

        public PolicyControllerTest()
        {
            _mockService = new Mock<IPolicyService>();
            _controller = new PolicyController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _mockService.Setup(s => s.GetAllPolicyGroupAsync())
                .ReturnsAsync(new List<PolicyGroup> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllGroupById_ReturnsOk()
        {
            _mockService.Setup(s => s.GetAllPoliciesByGroupAsync(1))
                .ReturnsAsync(new List<Policy> { new() });

            var result = await _controller.GetAllGroupById(1);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}
