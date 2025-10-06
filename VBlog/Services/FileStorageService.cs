using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace VBlog.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Yüklenen bir resmi belirli bir alt klasöre kaydeder.
        /// </summary>
        /// <param name="file">Yüklenecek dosya.</param>
        /// <param name="subfolder">Resmin kaydedileceği wwwroot/images altındaki klasör (örneğin "posts" veya "comments").</param>
        /// <returns>Kaydedilen resmin göreceli yolu (örneğin "/images/posts/guid.jpg") veya hata durumunda null.</returns>
        public async Task<string?> SaveImageAsync(IFormFile? file, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                return null; // Dosya yoksa veya boşsa resim yolu döndürme
            }

            // Dosya boyutu kontrolü
            if (file.Length > MaxFileSize)
            {
                throw new InvalidOperationException($"Dosya boyutu {MaxFileSize / (1024 * 1024)} MB'dan büyük olamaz.");
            }

            // Dosya uzantısı kontrolü
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException($"Sadece {string.Join(", ", _allowedExtensions)} uzantılı dosyalar yüklenebilir.");
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subfolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{subfolder}/{uniqueFileName}"; // wwwroot'a göre göreceli yol
        }

        /// <summary>
        /// Belirtilen yoldaki resmi siler.
        /// </summary>
        /// <param name="imagePath">Silinecek resmin göreceli yolu (örneğin "/images/posts/guid.jpg").</param>
        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return;
            }

            // Path.GetFileName ile güvenlik sağlamak için dosya adını alıp Combine etmek daha güvenli.
            // Ancak bu örnekte doğrudan imagePath'in wwwroot'a göre olduğu varsayılıyor.
            // Gerçek bir uygulamada path traversal saldırılarına karşı daha dikkatli olunmalı.
            var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
    }
}