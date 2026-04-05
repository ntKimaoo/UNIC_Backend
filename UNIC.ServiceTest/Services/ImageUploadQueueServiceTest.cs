using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ImageUploadQueueServiceTest
    {
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
                    PostId = i,
                    FileName = $"test{i}.jpg",
                    FileData = new byte[] { 0x01 },
                    Folder = "folder"
                });
            }
        }

        #endregion
    }
}
