using System.Text.RegularExpressions;
using IPTS.Resources;
using Microsoft.AspNetCore.Http;

namespace IPTS.Services
{
    public interface IFileService
    {
        /// <summary>
        /// يحفظ ملف الوصفة الطبية في المجلد الخارجي InternalStorage/Prescriptions
        /// </summary>
        Task<string?> SavePrescriptionFileAsync(IFormFile file);

        /// <summary>
        /// يحذف ملف الوصفة الطبية من السيرفر
        /// </summary>
        Task<bool> DeletePrescriptionFileAsync(string? fileName);

        /// <summary>
        /// يقرأ ملف الوصفة الطبية ويعيده كـ bytes
        /// </summary>
        Task<(byte[]? Content, string? ContentType, string? FileName)> GetPrescriptionFileAsync(string fileName);

        /// <summary>
        /// يتحقق من صحة الملف (الحجم والامتداد)
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidatePrescriptionFile(IFormFile file);
    }

    public class FileService : IFileService
    {
        private readonly LocService _locService;
        private readonly string _prescriptionStoragePath;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileService(LocService locService, IWebHostEnvironment env)
        {
            _locService = locService;
            // Create path inside project root
            _prescriptionStoragePath = Path.Combine(
                env.ContentRootPath,
                "InternalStorage",
                "Prescriptions"
            );

            // Ensure directory exists
            if (!Directory.Exists(_prescriptionStoragePath))
            {
                Directory.CreateDirectory(_prescriptionStoragePath);
            }
        }

        public (bool IsValid, string ErrorMessage) ValidatePrescriptionFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, _locService.GetSystem("File_MustBeSelected"));

            if (file.Length > MaxFileSize)
                return (false, string.Format(_locService.GetSystem("File_SizeExceededWithCurrent"), file.Length / (1024 * 1024)));

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
                return (false, _locService.GetSystem("File_UnsupportedTypePrescription"));

            return (true, string.Empty);
        }

        public async Task<string?> SavePrescriptionFileAsync(IFormFile file)
        {
            try
            {
                // Validate file
                var (isValid, errorMessage) = ValidatePrescriptionFile(file);
                if (!isValid)
                    throw new ArgumentException(errorMessage);

                // Clean original filename from invalid characters
                var originalName = CleanFileName(Path.GetFileNameWithoutExtension(file.FileName));
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                // Generate filename with GUID
                var fileName = $"{originalName}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(_prescriptionStoragePath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return fileName;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeletePrescriptionFileAsync(string? fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return true;

                var filePath = Path.Combine(_prescriptionStoragePath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return await Task.FromResult(true);
                }

                return true; // File doesn't exist, consider it success
            }
            catch
            {
                return false;
            }
        }

        public async Task<(byte[]? Content, string? ContentType, string? FileName)> GetPrescriptionFileAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return (null, null, null);

                var filePath = Path.Combine(_prescriptionStoragePath, fileName);

                if (!File.Exists(filePath))
                    return (null, null, null);

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(filePath);

                return (fileBytes, contentType, fileName);
            }
            catch
            {
                return (null, null, null);
            }
        }

        private string CleanFileName(string fileName)
        {
            // Remove invalid characters
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegex = new Regex($"[{invalidChars}]");
            var cleaned = invalidRegex.Replace(fileName, "");

            // Remove extra spaces and replace them with underscore
            cleaned = Regex.Replace(cleaned, @"\s+", "_");

            return cleaned;
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
