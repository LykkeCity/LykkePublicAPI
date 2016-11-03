using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Storage;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob
{
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly CloudBlobClient _blobClient;

        public AzureBlobStorage(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();

        }

        public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream,
            bool anonymousAccess = false)
        {
            var blockBlob = await GetBlockBlobReference(container, key, anonymousAccess);

            bloblStream.Position = 0;
            await blockBlob.UploadFromStreamAsync(bloblStream);

            return blockBlob.Uri.AbsoluteUri;
        }

        public async Task<string> SaveBlobAsync(string container, string key, byte[] blob, bool anonymousAccess = false)
        {
            var blockBlob = await GetBlockBlobReference(container, key, anonymousAccess);
            await blockBlob.UploadFromByteArrayAsync(blob, 0, blob.Length);

            return blockBlob.Uri.AbsoluteUri;
        }

        private async Task<CloudBlockBlob> GetBlockBlobReference(string container, string key, bool anonymousAccess)
        {
            var containerRef = _blobClient.GetContainerReference(container);

            if (!await containerRef.ExistsAsync())
            {
                await containerRef.CreateAsync();
                if (anonymousAccess)
                {
                    BlobContainerPermissions permissions = await containerRef.GetPermissionsAsync();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    await containerRef.SetPermissionsAsync(permissions);
                }
            }

            return containerRef.GetBlockBlobReference(key);
        }

        public Task<bool> HasBlobAsync(string container, string key)
        {
            var containerRef = _blobClient.GetContainerReference(container).GetBlobReference(key);
            return containerRef.ExistsAsync();
        }

        public async Task<DateTime?> GetBlobsLastModifiedAsync(string container)
        {
            var containerRef = _blobClient.GetContainerReference(container);

            BlobContinuationToken continuationToken = null;

            DateTime? lastModDateTime = null;
            do
            {
                var response = await containerRef.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                var dtOffset = response.Results.Where(x => x is CloudBlob).Max(x => ((CloudBlob)x).Properties.LastModified);

                var dt = dtOffset?.UtcDateTime;

                if (dt != null && (lastModDateTime == null || lastModDateTime < dt))
                    lastModDateTime = dt.Value;
            }
            while (continuationToken != null);

            return lastModDateTime;
        }

        public async Task<Stream> GetAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            var ms = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(ms);
            ms.Position = 0;
            return ms;
        }

        public async Task<string> GetAsTextAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            return await blockBlob.DownloadTextAsync();
        }

        public string GetBlobUrl(string container, string key)
        {
            var containerRef = _blobClient.GetContainerReference(container);
            var blockBlob = containerRef.GetBlockBlobReference(key);

            return blockBlob.Uri.AbsoluteUri;
        }

        public Task DelBlobAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            return blockBlob.DeleteAsync();
        }

    }
}
