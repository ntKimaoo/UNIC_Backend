using BusinessLogic.Services.Implementation;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class FileStorageServiceTest
    {
        private readonly Mock<Cloudinary> _mockCloudinary;
        private readonly FileStorageService _service;

        public FileStorageServiceTest()
        {
            var configData = new Dictionary<string, string?>
            {
                { "FileUpload:MaxFileSizeInMB", "5" },
                { "FileUpload:AllowedImageExtensions:0", ".jpg" },
                { "FileUpload:AllowedImageExtensions:1", ".jpeg" },
                { "FileUpload:AllowedImageExtensions:2", ".png" },
                { "FileUpload:AllowedImageExtensions:3", ".gif" },
                { "FileUpload:AllowedImageExtensions:4", ".webp" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _mockCloudinary = new Mock<Cloudinary>(new Account("cloud", "key", "secret"));
            _service = new FileStorageService(configuration, _mockCloudinary.Object);
        }

        private static IFormFile CreateMockFile(string fileName, long size)
        {
            var stream = new MemoryStream(new byte[size > 0 ? size : 1]);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(size);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            return mockFile.Object;
        }

        #region ValidateImageFile

        [Fact]
        public void ValidateImageFile_ReturnsFalse_WhenFileIsNull()
        {
            var result = _service.ValidateImageFile(null!);
            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ReturnsFalse_WhenFileIsEmpty()
        {
            var file = CreateMockFile("test.jpg", 0);
            var result = _service.ValidateImageFile(file);
            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ReturnsFalse_WhenFileTooLarge()
        {
            var file = CreateMockFile("test.jpg", 6 * 1024 * 1024); // 6MB > 5MB
            var result = _service.ValidateImageFile(file);
            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ReturnsFalse_WhenExtensionNotAllowed()
        {
            var file = CreateMockFile("test.exe", 1024);
            var result = _service.ValidateImageFile(file);
            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ReturnsTrue_WhenValid()
        {
            var file = CreateMockFile("photo.jpg", 1024);
            var result = _service.ValidateImageFile(file);
            Assert.True(result);
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".webp")]
        public void ValidateImageFile_ReturnsTrue_ForAllAllowedExtensions(string ext)
        {
            var file = CreateMockFile($"photo{ext}", 1024);
            var result = _service.ValidateImageFile(file);
            Assert.True(result);
        }

        #endregion

        #region SaveFileAsync

        [Fact]
        public async Task SaveFileAsync_ThrowsInvalidOperation_WhenFileInvalid()
        {
            var file = CreateMockFile("bad.exe", 1024);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SaveFileAsync(file, "test-folder"));
        }

        [Fact]
        public async Task SaveFileAsync_ThrowsInvalidOperation_WhenFileTooLarge()
        {
            var file = CreateMockFile("big.jpg", 10 * 1024 * 1024); // 10MB > 5MB
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SaveFileAsync(file, "test-folder"));
        }

        [Fact]
        public async Task SaveFileAsync_ThrowsInvalidOperation_WhenFileNull()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SaveFileAsync(null!, "test-folder"));
        }

        #endregion

        #region DeleteFileAsync

        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenUrlIsNullOrEmpty()
        {
            Assert.False(await _service.DeleteFileAsync(null!));
            Assert.False(await _service.DeleteFileAsync(string.Empty));
        }

        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            // Invalid URL will throw an exception
            var result = await _service.DeleteFileAsync("not-a-valid-url");
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenUrlIsMalformed()
        {
            var result = await _service.DeleteFileAsync("ftp://invalid/url");
            Assert.False(result);
        }

        #endregion

        #region ValidateImageFile_EdgeCases

        [Fact]
        public void ValidateImageFile_CaseInsensitive_Extension()
        {
            var file = CreateMockFile("photo.JPG", 1024);
            var result = _service.ValidateImageFile(file);
            Assert.True(result);
        }

        [Fact]
        public void ValidateImageFile_ReturnsFalse_ForDocFiles()
        {
            var file = CreateMockFile("document.pdf", 1024);
            Assert.False(_service.ValidateImageFile(file));
        }

        [Fact]
        public void ValidateImageFile_ReturnsFalse_ForNoExtension()
        {
            var file = CreateMockFile("photo", 1024);
            Assert.False(_service.ValidateImageFile(file));
        }

        #endregion

        #region Constructor_Defaults

        [Fact]
        public void Constructor_UsesDefaults_WhenConfigNotSet()
        {
            var emptyConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var mockCloudinary = new Mock<Cloudinary>(new Account("cloud", "key", "secret"));
            var service = new FileStorageService(emptyConfig, mockCloudinary.Object);

            // Should use default extensions
            var validFile = CreateMockFile("photo.jpg", 1024);
            Assert.True(service.ValidateImageFile(validFile));
        }

        #endregion
    }
}
