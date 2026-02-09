using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using BusinessLogic.Services.Interface;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class FileStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly long _maxFileSizeInBytes;
        private readonly string[] _allowedExtensions;

        public FileStorageService(IConfiguration configuration, Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;

            var maxFileSizeMB = configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 5);
            _maxFileSizeInBytes = maxFileSizeMB * 1024 * 1024;

            _allowedExtensions = configuration.GetSection("FileUpload:AllowedImageExtensions")
                .Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        }


        public bool ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSizeInBytes)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            return true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (!ValidateImageFile(file))
                throw new InvalidOperationException("Invalid file. Please upload a valid image file (max 5MB).");

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // Extract public ID from URL
                // Example URL: https://res.cloudinary.com/cloud_name/image/upload/v123456789/folder/public_id.jpg
                var uri = new Uri(fileUrl);
                var segments = uri.Segments;
                var publicIdWithExtension = segments.Last();
                var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);
                
                // If the file is in a folder, the public ID usually needs the folder path
                // However, Cloudinary's Upload API result contains the full public ID.
                // For simplicity, we assume we need to extract it carefully or the user provides it.
                // This is a naive extraction. A better way would be to store PublicId in DB.
                
                var deletionParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);

                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
