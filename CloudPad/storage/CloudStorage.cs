using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    class CloudStorage : ICloudStorage
    {
        public CloudStorageAccount Account { get; }

        public CloudStorage(CloudStorageAccount account)
        {
            Account = account;
        }

        private CloudQueueClient _queueClient;
        public CloudQueueClient GetQueueClient()
        {
            return _queueClient ?? (_queueClient = Account.CreateCloudQueueClient());
        }

        private CloudBlobClient _blobClient;
        public CloudBlobClient GetBlobClient()
        {
            return _blobClient ?? (_blobClient = Account.CreateCloudBlobClient());
        }

        /// <summary>
        /// Put a storage queue message in the queue specified by queueName. The queue message will be serialized as JSON. If you pass a CloudQueueMessage instance, the message is added to the queue as-is (not serialized as JSON).
        /// </summary>
        public async Task<CloudQueueMessage> AddMessageAsync(string queueName, object message, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null)
        {
            var message2 = message as CloudQueueMessage;
            if (message2 == null)
            {
                message2 = new CloudQueueMessage(JsonConvert.SerializeObject(message));
            }
            var queueClient = GetQueueClient();
            var queue = queueClient.GetQueueReference(queueName);
            try
            {
                await queue.AddMessageAsync(message2, timeToLive, initialVisibilityDelay, null, null);
                return message2; // ok, done
            }
            catch (StorageException ex) when (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "QueueNotFound")
            {
                // ok, create queue
            }
            await queue.CreateIfNotExistsAsync();
            await queue.AddMessageAsync(message2, timeToLive, initialVisibilityDelay, null, null);
            return message2;
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        public async Task<CloudBlockBlob> UploadFromStreamAsync(string containerName, string blobName, Stream source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = GetBlobClient();
            var container = client.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);
            try
            {
                await blob.UploadFromStreamAsync(source, cancellationToken);
                return blob; // ok, done
            }
            catch (StorageException ex) when (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "ContainerNotFound")
            {
                // ok, create container
            }
            await container.CreateIfNotExistsAsync();
            await blob.UploadFromStreamAsync(source, cancellationToken);
            return blob; // ok, done
        }
    }
}
