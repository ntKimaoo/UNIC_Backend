using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Background
{
    public class ImageUploadQueueService : BackgroundService
    {
        private readonly ILogger<ImageUploadQueueService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentQueue<ImageUploadTask> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public ImageUploadQueueService(ILogger<ImageUploadQueueService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public static void EnqueueTask(ImageUploadTask task)
        {
            _queue.Enqueue(task);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image Upload Queue Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_queue.TryDequeue(out var task))
                    {
                        await ProcessUploadAsync(task);
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing image upload queue.");
                    try { await Task.Delay(5000, stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("Image Upload Queue Service is stopping.");
        }

        private async Task ProcessUploadAsync(ImageUploadTask task)
        {
            using var scope = _serviceProvider.CreateScope();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
            var clubPostRepository = scope.ServiceProvider.GetRequiredService<IClubPostRepository>();

            try
            {
                _logger.LogInformation("Processing image upload for Post ID: {PostId}", task.PostId);

                // Convert byte array back to IFormFile-like stream for the storage service
                using (var stream = new MemoryStream(task.FileData))
                {
                    // IFileStorageService implementation expects IFormFile, 
                    // but we can refactor SaveFileAsync to take a stream or use a wrapper.
                    // For now, let's assume we might need a small adjustment in IFileStorageService 
                    // or just use Cloudinary directly here. 
                    // Let's stick to architectural consistency and use Cloudinary via FileStorageService if possible.
                    
                    // Since IFileStorageService uses IFormFile, let's add an overload or helper.
                    // Actually, I'll just use the Cloudinary SDK directly here to keep it simple for background processing.
                    
                    // BETTER: Refactor FileStorageService to handle Stream. 
                    // But to avoid blocking the UI response, I'll just use the Cloudinary logic here.
                    
                    var cloudinary = scope.ServiceProvider.GetRequiredService<CloudinaryDotNet.Cloudinary>();
                    
                    var uploadParams = new CloudinaryDotNet.Actions.ImageUploadParams
                    {
                        File = new CloudinaryDotNet.FileDescription(task.FileName, stream),
                        Folder = task.Folder,
                        Transformation = new CloudinaryDotNet.Transformation().Quality("auto").FetchFormat("auto")
                    };

                    var uploadResult = await cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error == null)
                    {
                        var post = await clubPostRepository.GetByIdAsync(task.PostId);
                        if (post != null)
                        {
                            post.ImageUrl = uploadResult.SecureUrl.ToString();
                            post.Status = "PUBLISHED";
                            await clubPostRepository.UpdateAsync(post);
                            _logger.LogInformation("Successfully uploaded image and published Post ID: {PostId}", task.PostId);
                        }
                    }
                    else
                    {
                        _logger.LogError("Cloudinary upload failed for Post ID {PostId}: {Error}", task.PostId, uploadResult.Error.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image upload for Post ID: {PostId}", task.PostId);
            }
        }
    }
}
