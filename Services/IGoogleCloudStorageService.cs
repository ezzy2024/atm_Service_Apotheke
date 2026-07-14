using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public interface IGoogleCloudStorageService
    {
        Task<string> UploadDocumentAsync(Stream sourceStream, string originalFilename, string contentType, CancellationToken ct = default);
        Task<Stream> DownloadDocumentAsync(string objectName, CancellationToken ct = default);
    }
}
