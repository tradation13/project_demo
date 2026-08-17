using System.Text.RegularExpressions;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Resources;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace IPTS.Services
{
    public class MedicalCaseTestPhotoService
    {
        private readonly ApplicationDbContext _context;
        private readonly LocService _locService;
        private readonly string _storagePath;
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSize = 5 * 1024 * 1024;

        public MedicalCaseTestPhotoService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            LocService locService)
        {
            _context = context;
            _locService = locService;
            _storagePath = Path.Combine(env.ContentRootPath, "InternalStorage", "MedicalCasePhotos");
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }

        public async Task<(bool Success, string? Error)> SaveOrReplaceAsync(
            int medicalCaseId,
            int testId,
            int photoKind,
            int slot,
            IFormFile? file)
        {
            if (photoKind is not (0 or 1))
                return (false, _locService.GetSystem("TestPhoto_InvalidKind"));

            if (slot is not (1 or 2))
                return (false, _locService.GetSystem("TestPhoto_InvalidSlot"));

            var testInCase = await _context.MedicalCaseTests
                .AnyAsync(t => t.MedicalCaseId == medicalCaseId && t.TestId == testId);
            if (!testInCase)
                return (false, _locService.GetSystem("TestPhoto_TestNotInCase"));

            var validation = ValidateImage(file);
            if (!validation.IsValid)
                return (false, validation.Error);

            var newFileName = await SaveFileAsync(file!);
            if (string.IsNullOrWhiteSpace(newFileName))
                return (false, _locService.GetSystem("TestPhoto_UploadFailed"));

            var existing = await _context.MedicalCaseTestPhotos.FirstOrDefaultAsync(p =>
                p.MedicalCaseId == medicalCaseId
                && p.TestId == testId
                && p.PhotoKind == photoKind
                && p.Slot == slot);

            if (existing != null)
            {
                DeletePhysicalFile(existing.FileName);
                existing.FileName = newFileName;
                existing.OriginalFileName = Path.GetFileName(file!.FileName);
                existing.CreatedAt = DateTime.UtcNow;
                _context.MedicalCaseTestPhotos.Update(existing);
            }
            else
            {
                await _context.MedicalCaseTestPhotos.AddAsync(new MedicalCaseTestPhoto
                {
                    MedicalCaseId = medicalCaseId,
                    TestId = testId,
                    PhotoKind = photoKind,
                    Slot = slot,
                    FileName = newFileName,
                    OriginalFileName = Path.GetFileName(file!.FileName),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, int? MedicalCaseId, string? Error)> DeleteAsync(int photoId)
        {
            var photo = await _context.MedicalCaseTestPhotos.FirstOrDefaultAsync(p => p.Id == photoId);
            if (photo == null)
                return (false, null, _locService.GetSystem("TestPhoto_NotFound"));

            var medicalCaseId = photo.MedicalCaseId;
            DeletePhysicalFile(photo.FileName);
            _context.MedicalCaseTestPhotos.Remove(photo);
            await _context.SaveChangesAsync();
            return (true, medicalCaseId, null);
        }

        public async Task DeletePileIfOrphanedAsync(int medicalCaseId, int testId)
        {
            var stillHasTests = await _context.MedicalCaseTests
                .AnyAsync(t => t.MedicalCaseId == medicalCaseId && t.TestId == testId);
            if (stillHasTests)
                return;

            var photos = await _context.MedicalCaseTestPhotos
                .Where(p => p.MedicalCaseId == medicalCaseId && p.TestId == testId)
                .ToListAsync();

            foreach (var photo in photos)
                DeletePhysicalFile(photo.FileName);

            if (photos.Count == 0)
                return;

            _context.MedicalCaseTestPhotos.RemoveRange(photos);
            await _context.SaveChangesAsync();

            LogHelper.LogWithContext(
                $"Deleted {photos.Count} orphaned comparison photos for case {medicalCaseId}, test {testId}",
                "system",
                "Doctor",
                "MedicalCaseTestPhotoService.DeletePileIfOrphanedAsync",
                LogEventLevel.Information);
        }

        public async Task<MedicalCaseTestPhoto?> GetByFileNameAsync(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
                return null;

            return await _context.MedicalCaseTestPhotos
                .Include(p => p.MedicalCase)
                .FirstOrDefaultAsync(p => p.FileName == safeFileName);
        }

        public (FileStream? Stream, string ContentType)? OpenPhotoFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
                return null;

            var path = Path.Combine(_storagePath, safeFileName);
            var fullPath = Path.GetFullPath(path);
            var fullFolder = Path.GetFullPath(_storagePath);
            if (!fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!File.Exists(fullPath))
                return null;

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (stream, GetContentType(fullPath));
        }

        private async Task<string?> SaveFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return null;

            var safeName = CleanFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var fileName = $"{safeName}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_storagePath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        private void DeletePhysicalFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_storagePath, safeFileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private (bool IsValid, string Error) ValidateImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (false, _locService.GetSystem("File_Empty"));
            if (file.Length > MaxFileSize)
                return (false, _locService.GetSystem("File_TooLarge"));
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension)
                ? (true, string.Empty)
                : (false, _locService.GetSystem("File_InvalidExtension"));
        }

        private static string CleanFileName(string fileName)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegex = new Regex($"[{invalidChars}]");
            var cleaned = invalidRegex.Replace(fileName, string.Empty);
            cleaned = Regex.Replace(cleaned, @"\s+", "_");
            return string.IsNullOrWhiteSpace(cleaned) ? "photo" : cleaned;
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
