using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Interface;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ImageUploadQueueServiceTest
    {
        private readonly Mock<ILogger<ImageUploadQueueService>> _mockLogger;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly Mock<IClubPostRepository> _mockClubPostRepo;
        private readonly Mock<Cloudinary> _mockCloudinary;
        private readonly IServiceProvider _serviceProvider;

        public ImageUploadQueueServiceTest()
        {
            _mockLogger = new Mock<ILogger<ImageUploadQueueService>>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockClubPostRepo = new Mock<IClubPostRepository>();
            _mockCloudinary = new Mock<Cloudinary>(new Account("cloud", "key", "secret"));

            var services = new ServiceCollection();
            services.AddSingleton(_mockFileStorage.Object);
            services.AddSingleton(_mockClubPostRepo.Object);
            services.AddSingleton(_mockCloudinary.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        #region EnqueueTask

        [Fact]
        public void EnqueueTask_AddsTaskToQueue()
        {
            var task = new ImageUploadTask
            {
                PostId = 1,
                FileName = "test.jpg",
                FileData = new byte[] { 0x01, 0x02 },
                Folder = "test-folder"
            };

            // Should not throw
            ImageUploadQueueService.EnqueueTask(task);
        }

        [Fact]
        public void EnqueueTask_MultipleItems_DoesNotThrow()
        {
            for (int i = 0; i < 10; i++)
            {
                ImageUploadQueueService.EnqueueTask(new ImageUploadTask
                {
                    PostId = i + 100,
                    FileName = $"test{i}.jpg",
                    FileData = new byte[] { 0x01 },
                    Folder = "folder"
                });
            }
        }

        #endregion

        #region ExecuteAsync

        [Fact]
        public async Task ExecuteAsync_StopsGracefully_WhenCancelled()
        {
            var service = new ImageUploadQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(200);
            await service.StartAsync(cts.Token);
            await Task.Delay(300);
            await service.StopAsync(CancellationToken.None);
        }

        #endregion
    }
}
