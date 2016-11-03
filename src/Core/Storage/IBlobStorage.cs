using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core.Storage
{
    public interface IBlobStorage
    {
        Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false);
        Task<string> SaveBlobAsync(string container, string key, byte[] blob, bool anonymousAccess = false);

        Task<bool> HasBlobAsync(string container, string key);

        /// <summary>
        /// Returns datetime of latest modification among all blobs
        /// </summary>
        Task<DateTime?> GetBlobsLastModifiedAsync(string container);

        Task<Stream> GetAsync(string blobContainer, string key);
        Task<string> GetAsTextAsync(string blobContainer, string key);

        string GetBlobUrl(string container, string key);

        Task DelBlobAsync(string blobContainer, string key);
    }

}
