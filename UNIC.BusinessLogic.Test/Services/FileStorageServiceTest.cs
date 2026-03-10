using BusinessLogic.Services.Implementation;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class FileStorageServiceTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FileStorageService _fileStorageService;

        public FileStorageServiceTest()
        {
            _mockConfig = new Mock<IConfiguration>();

            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns("");
            
            // Set up basic config for file validation
            // Value is fetched using GetValue<int> and Get<string[]> which are extension methods, 
            // but we can mock the direct indexing if we don't use extensions, or set up in-memory config.
            // A simpler way is to just let the service read default values from the extensions if the mock returns null.
            _mockConfig.Setup(c => c.GetSection("FileUpload:AllowedImageExtensions")).Returns(mockSection.Object);
            
            // We pass null for Cloudinary as we are mainly testing the validation logic 
            // and avoiding real network calls to Cloudinary API.
            _fileStorageService = new FileStorageService(_mockConfig.Object, null!);
        }

        #region ValidateImageFile

        [Fact]
        public void ValidateImageFile_ShouldReturnFalse_WhenFileIsNull()
        {
            var result = _fileStorageService.ValidateImageFile(null!);
            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ShouldReturnFalse_WhenFileIsEmpty()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            var result = _fileStorageService.ValidateImageFile(mockFile.Object);

            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ShouldReturnFalse_WhenFileIsTooLarge()
        {
            // By default MaxFileSizeInMB is 5 if configuration doesn't provide it via GetValue.
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            var result = _fileStorageService.ValidateImageFile(mockFile.Object);

            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ShouldReturnFalse_WhenExtensionIsInvalid()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns("test.pdf"); // Invalid extension

            var result = _fileStorageService.ValidateImageFile(mockFile.Object);

            Assert.False(result);
        }

        [Fact]
        public void ValidateImageFile_ShouldReturnTrue_WhenFileIsValid()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns("test.png"); // Valid extension

            var result = _fileStorageService.ValidateImageFile(mockFile.Object);

            Assert.True(result);
        }

        #endregion

        #region SaveFileAsync

        [Fact]
        public async Task SaveFileAsync_ShouldThrowException_WhenValidationFails()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB (invalid)

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _fileStorageService.SaveFileAsync(mockFile.Object, "folder"));
            Assert.Contains("Invalid file", ex.Message);
        }

        #endregion

        #region DeleteFileAsync

        [Fact]
        public async Task DeleteFileAsync_ShouldReturnFalse_WhenUrlIsNullOrEmpty()
        {
            var result1 = await _fileStorageService.DeleteFileAsync(null!);
            var result2 = await _fileStorageService.DeleteFileAsync("");

            Assert.False(result1);
            Assert.False(result2);
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldReturnFalse_OnException()
        {
            // Passing an invalid URL to trigger catch block
            var result = await _fileStorageService.DeleteFileAsync("not-a-valid-url");
            Assert.False(result);
        }

        #endregion
    }
}
