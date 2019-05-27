using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad
{
    public interface ICloudStorage
    {
        CloudStorageAccount Account { get; }

        // ================================

        CloudQueueClient GetQueueClient();

        /// <summary>
        /// Put a storage queue message in the queue specified by queueName. The queue message will be serialized as JSON. If you pass a CloudQueueMessage instance, the message is added to the queue as-is (not serialized as JSON).
        /// </summary>
        Task<CloudQueueMessage> AddMessageAsync(string queueName, object message, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null);

        // ================================

        CloudBlobClient GetBlobClient();

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        Task<CloudBlockBlob> UploadFromStreamAsync(string containerName, string blobName, Stream source, CancellationToken cancellationToken = default(CancellationToken));
    }
}
