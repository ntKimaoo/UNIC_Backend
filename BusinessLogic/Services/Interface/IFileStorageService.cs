using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string fileUrl);
        bool ValidateImageFile(IFormFile file);
    }
}
