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
        /// يفتح ملف الوصفة للبث دون تحميله كاملًا في الذاكرة.
        /// </summary>
        (FileStream? Stream, string? ContentType, string? FileName) OpenPrescriptionFile(string fileName);

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

                var safeFileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
                    return true;

                var filePath = Path.Combine(_prescriptionStoragePath, safeFileName);
                var fullPath = Path.GetFullPath(filePath);
                var fullFolder = Path.GetFullPath(_prescriptionStoragePath);
                if (!fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return await Task.FromResult(true);
                }

                return true; // File doesn't exist, consider it success
            }
            catch
            {
                return false;
            }
        }

        public (FileStream? Stream, string? ContentType, string? FileName) OpenPrescriptionFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return (null, null, null);

                var safeFileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
                    return (null, null, null);

                var filePath = Path.Combine(_prescriptionStoragePath, safeFileName);
                var fullPath = Path.GetFullPath(filePath);
                var fullFolder = Path.GetFullPath(_prescriptionStoragePath);
                if (!fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                    return (null, null, null);

                if (!File.Exists(fullPath))
                    return (null, null, null);

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return (stream, GetContentType(fullPath), safeFileName);
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
