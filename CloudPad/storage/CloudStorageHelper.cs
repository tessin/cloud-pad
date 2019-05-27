using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CloudPad {
  public class CloudStorageHelper {
    private readonly CloudStorageAccount _account;

    public CloudStorageHelper(CloudStorageAccount account) {
      _account = account;
    }

    private CloudQueueClient _queueClient;
    public CloudQueueClient GetQueueClient() {
      return _queueClient ?? (_queueClient = _account.CreateCloudQueueClient());
    }

    /// <summary>
    /// Put a storage queue message in the queue specified by queueName. The queue message will be serialized as JSON. If you pass a CloudQueueMessage instance, the message is added to the queue as-is (not serialized as JSON).
    /// </summary>
    public async Task AddQueueMessageAsync(string queueName, object message, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null) {
      var message2 = message as CloudQueueMessage;
      if (message2 == null) {
        message2 = new CloudQueueMessage(JsonConvert.SerializeObject(message));
      }
      var queueClient = GetQueueClient();
      var queue = queueClient.GetQueueReference(queueName);
      await queue.AddMessageAsync(message2, timeToLive, initialVisibilityDelay, null, null);
    }
  }
}
