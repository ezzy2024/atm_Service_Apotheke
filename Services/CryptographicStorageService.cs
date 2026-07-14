using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ServiceApotheke.API.Services
{
    public interface ICryptographicStorageService
    {
        Task<string> EncryptAndStoreAsync(Stream sourceStream, string originalFilename, CancellationToken ct = default);
        Task<byte[]> RetrieveAndDecryptAsync(string locatorPath, CancellationToken ct = default);
    }

    public class LocalEncryptedStorageProvider : ICryptographicStorageService
    {
        private readonly byte[] _encryptionKey;
        private readonly string _storageDirectory;

        public LocalEncryptedStorageProvider(IConfiguration configuration)
        {
            var keyBase64 = configuration["DMS:AesEncryptionKey"] ?? "b2VFRURHREVDc1Y5RElKT3gwbThYMWtzbnRTRkhUcmE="; // 32 bytes base64 for local dev
            _encryptionKey = Convert.FromBase64String(keyBase64);
            _storageDirectory = Path.Combine(Path.GetTempPath(), "DmsVault");

            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async Task<string> EncryptAndStoreAsync(Stream sourceStream, string originalFilename, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await sourceStream.CopyToAsync(ms, ct);
            var plaintext = ms.ToArray();

            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
            var ciphertext = new byte[plaintext.Length];

            using (var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var extension = Path.GetExtension(originalFilename);
            var locatorFileName = $"{Guid.NewGuid()}{extension}.enc";
            var fullPath = Path.Combine(_storageDirectory, locatorFileName);

            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fs.WriteAsync(nonce, 0, nonce.Length, ct);
            await fs.WriteAsync(tag, 0, tag.Length, ct);
            await fs.WriteAsync(ciphertext, 0, ciphertext.Length, ct);

            return locatorFileName;
        }

        public async Task<byte[]> RetrieveAndDecryptAsync(string locatorPath, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_storageDirectory, locatorPath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Encrypted blob not found.");

            var fileBytes = await File.ReadAllBytesAsync(fullPath, ct);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
            var tagSize = AesGcm.TagByteSizes.MaxSize;     // 16

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertext = new byte[fileBytes.Length - nonceSize - tagSize];

            Buffer.BlockCopy(fileBytes, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(fileBytes, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(fileBytes, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }
    }

    public class GcsEncryptedStorageProvider : ICryptographicStorageService
    {
        private readonly byte[] _encryptionKey;
        private readonly IGoogleCloudStorageService _gcsService;

        public GcsEncryptedStorageProvider(IConfiguration configuration, IGoogleCloudStorageService gcsService)
        {
            var keyBase64 = configuration["DMS:AesEncryptionKey"] ?? "b2VFRURHREVDc1Y5RElKT3gwbThYMWtzbnRTRkhUcmE=";
            _encryptionKey = Convert.FromBase64String(keyBase64);
            _gcsService = gcsService;
        }

        public async Task<string> EncryptAndStoreAsync(Stream sourceStream, string originalFilename, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await sourceStream.CopyToAsync(ms, ct);
            var plaintext = ms.ToArray();

            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            var ciphertext = new byte[plaintext.Length];

            using (var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var extension = Path.GetExtension(originalFilename);
            var locatorFileName = $"{Guid.NewGuid()}{extension}.enc";

            using var encryptedStream = new MemoryStream();
            await encryptedStream.WriteAsync(nonce, 0, nonce.Length, ct);
            await encryptedStream.WriteAsync(tag, 0, tag.Length, ct);
            await encryptedStream.WriteAsync(ciphertext, 0, ciphertext.Length, ct);
            encryptedStream.Position = 0;

            return await _gcsService.UploadDocumentAsync(encryptedStream, locatorFileName, "application/octet-stream", ct);
        }

        public async Task<byte[]> RetrieveAndDecryptAsync(string locatorPath, CancellationToken ct = default)
        {
            using var encryptedStream = await _gcsService.DownloadDocumentAsync(locatorPath, ct);
            using var ms = new MemoryStream();
            await encryptedStream.CopyToAsync(ms, ct);
            var fileBytes = ms.ToArray();

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertext = new byte[fileBytes.Length - nonceSize - tagSize];

            Buffer.BlockCopy(fileBytes, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(fileBytes, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(fileBytes, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }
    }
}
