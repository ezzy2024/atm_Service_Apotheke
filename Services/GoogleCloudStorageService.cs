using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceApotheke.API.Services
{
    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly ILogger<GoogleCloudStorageService> _logger;

        public GoogleCloudStorageService(IConfiguration configuration, ILogger<GoogleCloudStorageService> logger)
        {
            _logger = logger;
            _bucketName = configuration["GCS:BucketName"] ?? "serviceapotheke-documents-dev";
            
            // For local development, this will use Application Default Credentials.
            // In Cloud Run, it automatically uses the attached service account.
            try
            {
                _storageClient = StorageClient.Create();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Google Cloud Storage client. Make sure Application Default Credentials are set.");
                // For development fallback if ADC is missing, we could mock or throw, throwing for now to fail fast if used incorrectly.
                // Depending on environment, this might need handling.
            }
        }

        public async Task<string> UploadDocumentAsync(Stream sourceStream, string originalFilename, string contentType, CancellationToken ct = default)
        {
            if (_storageClient == null) throw new InvalidOperationException("StorageClient is not initialized.");

            var extension = Path.GetExtension(originalFilename);
            var objectName = $"{Guid.NewGuid()}{extension}";

            await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, sourceStream, cancellationToken: ct);
            
            _logger.LogInformation("Uploaded document {ObjectName} to bucket {BucketName}", objectName, _bucketName);
            return objectName;
        }

        public async Task<Stream> DownloadDocumentAsync(string objectName, CancellationToken ct = default)
        {
            if (_storageClient == null) throw new InvalidOperationException("StorageClient is not initialized.");

            var memoryStream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream, cancellationToken: ct);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
