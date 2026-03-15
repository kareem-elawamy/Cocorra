using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.Upload
{
    public class UploadImage : IUploadImage
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long _maxFileSize = 5 * 1024 * 1024;

        public UploadImage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0) return "Error:NoFile";
                if (imageFile.Length > _maxFileSize) return "Error:FileTooLarge";

                string extension = Path.GetExtension(imageFile.FileName).ToLower();
                if (!_allowedExtensions.Contains(extension)) return "Error:InvalidExtension";

                var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg", "image/webp" };
                if (!validTypes.Contains(imageFile.ContentType.ToLower()) && !imageFile.ContentType.StartsWith("image/"))
                {
                    return $"Error:InvalidFileType - Received: {imageFile.ContentType}";
                }

                if (!IsValidImageSignature(imageFile)) return "Error:FakeImage";

                string contentPath = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                    : _env.WebRootPath;

                string path = Path.Combine(contentPath, "Uploads", "img", "Profiles");

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(path, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                return Path.Combine("Uploads", "img", "Profiles", fileName).Replace("\\", "/");
            }
            catch (Exception)
            {
                return "Error:ServerException";
            }
        }
        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            try
            {
                string contentPath = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                    : _env.WebRootPath;

                var fullPath = Path.Combine(contentPath, imagePath.Replace("/", "\\"));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
            }
        }
        private bool IsValidImageSignature(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // البصمات الخاصة بأشهر أنواع الصور
                    var signatures = new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF }, // JPEG / JPG
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, // PNG
                new byte[] { 0x47, 0x49, 0x46, 0x38 } // GIF
            };

                    // نقرأ أول 8 بايت من الملف (لأن أكبر بصمة لدينا هي PNG وتتكون من 8 بايت)
                    byte[] headerBytes = new byte[8];
                    stream.Read(headerBytes, 0, headerBytes.Length);

                    // خطوة مهمة جداً: إعادة المؤشر لبداية الملف حتى يتم حفظه بشكل كامل لاحقاً
                    stream.Position = 0;

                    // نتحقق مما إذا كانت بداية الملف تتطابق مع أي من البصمات المعروفة
                    return signatures.Any(signature =>
                        headerBytes.Take(signature.Length).SequenceEqual(signature));
                }
            }
            catch
            {
                // في حالة حدوث أي خطأ في قراءة الملف، نعتبره غير صالح
                return false;
            }
        }
    }
}