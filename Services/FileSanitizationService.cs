using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ServiceApotheke.API.Services
{
    public interface IFileSanitizationService
    {
        bool IsValidDocument(IFormFile file);
    }

    public class FileSanitizationService : IFileSanitizationService
    {
        // Allowed max size: 5 MB
        private const int MaxFileSizeBytes = 5 * 1024 * 1024;

        // Magic bytes for common allowed formats
        private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] JpegMagic1 = { 0xFF, 0xD8, 0xFF, 0xE0 };
        private static readonly byte[] JpegMagic2 = { 0xFF, 0xD8, 0xFF, 0xE1 };
        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public bool IsValidDocument(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > MaxFileSizeBytes) return false;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png") return false;

            // MIME basic check
            var ct = file.ContentType.ToLowerInvariant();
            if (ct != "application/pdf" && ct != "image/jpeg" && ct != "image/png") return false;

            // Magic Bytes signature verification
            using var ms = new MemoryStream();
            file.CopyTo(ms);
            var headerBytes = ms.ToArray().Take(8).ToArray();

            if (ext == ".pdf" && StartsWith(headerBytes, PdfMagic)) return true;
            if ((ext == ".jpg" || ext == ".jpeg") && (StartsWith(headerBytes, JpegMagic1) || StartsWith(headerBytes, JpegMagic2))) return true;
            if (ext == ".png" && StartsWith(headerBytes, PngMagic)) return true;

            return false;
        }

        private bool StartsWith(byte[] source, byte[] magic)
        {
            if (source.Length < magic.Length) return false;
            for (int i = 0; i < magic.Length; i++)
            {
                if (source[i] != magic[i]) return false;
            }
            return true;
        }
    }
}
