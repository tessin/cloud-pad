# CloudPad 2.0 - run your LINQPad scripts as Azure Functions 🌩️

`CloudPad` is a NuGet package that you can use to build LINQPad scripts that run as Azure Functions. You can develop and test with LINQPad -- as you would expect -- and then publish your script to an Azure Function App that is running the CloudPad runtime (CloudPad.FunctionApp).

# Getting Started

There's minimal setup but you cannot just take any LINQPad script and run it as an Azure function, you need to have this bootstrapping snippet in your LINQPad program.

```cs
Task Main(string[] args) => Program.MainAsync(this, args);
```

Any public, non-static method defined in the LINQPad script is _potentially_ an Azure Function that can be invoked. As long as there is a supported trigger on top of the method, it should just work!

```cs
[HttpTrigger]
public HttpResponseMessage HelloHttp(HttpRequestMessage req)
{
  var res = req.CreateResponse(HttpStatusCode.OK);
  res.Content = new StringContent("Hello World!", Encoding.UTF8, "text/plain");
  return res;
}
```

# Supported Bindings

Technically, all the 1.x bindings are supported but we don't support intermingled input and output parameters. If you need this level of flex, go write an Azure Function instead.

https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings#supported-bindings

All triggers support the following parameter types.

- `CancellationToken`
- `ITraceWriter` wrapper around the `Microsoft.Azure.WebJobs.Host.TraceWriter` class
- `CloudStorageHelper` utilities for Azure Storage

## HttpTrigger

- `HttpRequestMessage`

While you can specify a route, you cannot get the route data by putting a parameter with the same name as a parameter to your function. (CloudPad does parameter binding differently).

## TimerTrigger

- `ITimerInfo` wrapper around the `Microsoft.Azure.WebJobs.TimerInfo` class

```cs
[TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
public void HelloTimer(ITimerInfo timer)
{
  timer.Dump();
}
```

## QueueTrigger

- `T` and/or `CloudQueueMessage` the first parameter of the function is used to infer a message data contract, if it isn't `CloudQueueMessage`.

When a message is put in the queue `"cloud-pad-queue"` we will attempt to deserialize it as JSON as the type specified by the first parameter, i.e. `Message`. If the type of the first parameter is `CloudQueueMessage` the message is passed as-is without going through deserialization.

```cs
public class Message
{
  public string Text { get; set; }
}

// the CloudStorageHelper is used to add a message to the queue. If the queue does not exist, it will be created for you.

[HttpTrigger]
public async Task HelloProducer(HttpRequestMessage req, CloudStorageHelper storage)
{
  await storage.AddQueueMessageAsync(QueueName, new Message { Text = "Hello World!" });
}


[QueueTrigger("cloud-pad-queue")]
public void HelloConsumer(Message msg)
{
  msg.Dump();
}
```

Note the exponential backoff policy. If a queue has been dormant for some time, it may take several minutes for the queue to be processed. Restarting the script may be the fastest way to get the processing moving during development.

## BlobTrigger

- `CloudBlockBlob`

When a blob is added or modified in the blob container `"cloud-pad-blob-container"` specified by the `BlogTrigger` the function will run.

```cs
[BlobTrigger("cloud-pad-blob-container")]
public void HelloBlob(CloudBlockBlob blob)
{
	blob.Uri.Dump("Got the blob!");
}
```

Note that we only support `CloudBlockBlob`, i.e. a typical file text and/or binary blob. You cannot use an `CloudAppendBlob` or `CloudPageBlob`, sorry.

# Examples

There are lot of examples in the `scripts` directory. Check them out!

# File Dependencies

There is a class called `FileDependency` in `CloudPad`. It can be used to bundle external files with scripts. The files are resolved relative the LINQPad script.

Note that you have to specify your file dependencies as instance members of your LINQPad query. You cannot create a `FileDependency` within a function and expect it to work. The `FileDependency` must be instantiated as your script is instantiated for it to work.

```cs
Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

readonly FileDependency html = new FileDependency("index.html"); // ok!
// static FileDependency html = new FileDependency("index.html"); // not ok!
// ^
// these cannot be static!

[HttpTrigger]
public HttpResponseMessage IndexHtml(HttpRequestMessage req)
{
  // var html = new FileDependency("index.html"); // not ok!

  var res = req.CreateResponse();
  res.Content = new StreamContent(html.OpenRead()) {
    Headers = {
      ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html")
    }
  };
  return res;
}
```

# NuGet Dependencies

CloudPad 2.0 was built with an explicit minimal set of dependencies to be compatible with the .NET Framework 4.6.1

- Microsoft.AspNet.WebApi.Core 5.2.3
- Newtonsoft.Json 9.0.1
- System.Net.Http 4.3.3
- WindowsAzure.Storage 7.2.1

You should be able to use additional NuGet packages, up to the .NET Standard 2.0 without conflict.

# Roadmap

CloudPad 3.0 will support .NET 5 (Core) on Windows and LINQPad 6 (assuming that the [next version of LINQPad](http://forum.linqpad.net/discussion/comment/4262/#Comment_4262) does support .NET Core). However, there is no rush as neither .NET 5 or LINQPad 6 has been released.
