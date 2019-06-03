# CloudPad 🌩️ 
## Run your LINQPad scripts as Azure Functions!

`CloudPad` is a NuGet package that you can use to build LINQPad scripts that run as Azure Functions. You can develop and test with LINQPad -- as you would expect -- and then publish your script to Azure.

# Getting Started

There's minimal setup but you cannot just take any LINQPad script and run it in Azure, you need to have this bootstrapping snippet in your LINQPad program.

```cs
Task Main(string[] args) => Program.MainAsync(this, args);
```

Any public, non-static method in the LINQPad script is _potentially_\* an Azure Function that can be invoked. As long as there is a supported trigger on top of the method, it should just work!

> **Note:** you have to make the method public and non-static (i.e. it has to be a public instance method).

# Supported Bindings

Technically, all the 1.x bindings are supported but we don't support intermingled input and output parameters. If you need this level of flex, go write an Azure Function instead.

https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings#supported-bindings

All triggers support the following parameter types.

- `CancellationToken`
- `ITraceWriter` wrapper around the `Microsoft.Azure.WebJobs.Host.TraceWriter` class
- `ICloudStorage` utilities for Azure Storage

## HttpTrigger

- `HttpRequestMessage`

```cs
[HttpTrigger]
public HttpResponseMessage HelloHttp(HttpRequestMessage req)
{
  var res = req.CreateResponse(HttpStatusCode.OK);
  res.Content = new StringContent("Hello World!", Encoding.UTF8, "text/plain");
  return res;
}
```

While you can specify a route, you cannot get the route data by putting a parameter with the same name as a parameter to your function. (CloudPad does parameter binding differently).

```cs
[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "Hello/{name}")]
public Task<HttpResponseMessage> HelloHttp(HttpRequestMessage req)
{
  // this won't work because of how to Azure Functions does things
  req.GetRouteData().Values.Dump();

 // this will work, GetRouteValue is an extension method from CloudPad
  var name = req.GetRouteValue("name", "");

  var res = req.CreateResponse(HttpStatusCode.OK);
  res.Content = new StringContent($"Hello {name}!", Encoding.UTF8, "text/plain");
  return Task.FromResult(res);
}
```

There is a similar interface for query string parameters.

```cs
[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "Hello")]
public Task<HttpResponseMessage> HelloHttpWithQuery(HttpRequestMessage req)
{
  // you can do this, or...
  req.GetQueryNameValuePairs().Dump();

  // you can do this GetQueryValue is an extension method provided by CloudPad (similar to GetRouteValue)
  var name = req.GetQueryValue("name", "", isRequired: true);

  // note that, by default query string parameters are not required and support multiple values, for example
  req.GetQueryValues<string>("name").Dump();

  // in the case of multiple values, `GetQueryValue` always gets the first value
  // GetQueryValue and GetQueryValues are not case sensitive

  var res = req.CreateResponse(HttpStatusCode.OK);
  res.Content = new StringContent($"Hello {name}!", Encoding.UTF8, "text/plain");
  return Task.FromResult(res);
}
```

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

// the ICloudStorage is used to add a message to the queue. If the queue does not exist, it will be created for you.

[HttpTrigger]
public async Task HelloQueueProducer(HttpRequestMessage req, ICloudStorage storage)
{
  await storage.AddMessageAsync(QueueName, new Message { Text = "Hello World!" });
}


[QueueTrigger("cloud-pad-queue")]
public void HelloQueueConsumer(Message msg)
{
  msg.Dump();
}
```

Note the exponential backoff policy. If a queue has been dormant for some time, it may take several minutes for the queue to be processed. Restarting the script may be the fastest way to get the processing moving during development.

## BlobTrigger

- `CloudBlockBlob`

When a blob is added or modified in the blob container `"cloud-pad-blob-container"` specified by the `BlogTrigger` the function will run.

```cs
[HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST", Route = "Upload")]
public async Task<HttpResponseMessage> HelloBlobProducer(HttpRequestMessage req, ICloudStorage storage)
{
  if (req.Method == HttpMethod.Post)
  {
    var data = await req.Content.ReadAsMultipartAsync();
    var payload = data.Contents.First(c => c.Headers.ContentDisposition.Dump().Name == "\"payload\"");
    var source = await payload.ReadAsStreamAsync();

    await storage.UploadFromStreamAsync(BlobContainer, payload.Headers.ContentDisposition.FileName.Trim('\"'), source);

    return req.CreateResponse(HttpStatusCode.NoContent); // thanks!
  }

  var res = req.CreateResponse(HttpStatusCode.OK);
  res.Content = new StringContent("<form method=POST enctype='multipart/form-data'><input type=file name=payload><input type=submit></form>", Encoding.UTF8, "text/html");
  return res;
}

[BlobTrigger(BlobContainer)]
public void HelloBlobConsumer(CloudBlockBlob blob)
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

You should be able to use additional NuGet packages, but there are issues with certain configurations. While CloudPad let's you deploy multiple LINQPad scripts to the same Azure Function host, this can cause an issue with dependencies and versions. Whenever possible. Try to target the .NET Framework 4.6.1 and don't use the most recent release of a NuGet package if it doesn't fix a specific problem for you. Try to use packages that are compatible with the dependencies of CloudPad (the Azure Functions host).

# Roadmap

CloudPad 3.0 will support .NET 5 (Core) on Windows and LINQPad 6 (assuming that the [next version of LINQPad](http://forum.linqpad.net/discussion/comment/4262/#Comment_4262) does support .NET Core). However, there is no rush as neither .NET 5 or LINQPad 6 has been released.
