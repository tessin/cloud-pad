<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Mono.Cecil.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Mono.Cecil.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Mono.Cecil.Mdb.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Mono.Cecil.Mdb.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Mono.Cecil.Pdb.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Mono.Cecil.Pdb.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Mono.Cecil.Rocks.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Mono.Cecil.Rocks.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Net.Http.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Algorithms.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Security.Cryptography.Algorithms.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Encoding.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Security.Cryptography.Encoding.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Primitives.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Security.Cryptography.Primitives.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.X509Certificates.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Security.Cryptography.X509Certificates.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Spatial.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Spatial.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Web.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Web.Http.dll</Reference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Blob</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

[HttpTrigger(AuthorizationLevel.Anonymous)]
public HttpResponseMessage HttpTest(HttpRequestMessage req)
{
	return req.CreateResponse("Hello World!");
}

[HttpTrigger(Route = "hello/{name}")]
public HttpResponseMessage HttpRouteValueTest(HttpRequestMessage req)
{
	var name = req.GetRouteValue<string>("name");

	return req.CreateResponse($"Hello {name}!");
}

[HttpTrigger]
public HttpResponseMessage HttpQueryValueTest(HttpRequestMessage req)
{
	var name = req.GetQueryValue<string>("name");

	return req.CreateResponse($"Query {name}!");
}

// ====

static Dictionary<string, TaskCompletionSource<int>> Pending = new Dictionary<string, TaskCompletionSource<int>>();

// ====

public class QueueMessage
{
	public string Id { get; set; }
}

[HttpTrigger("POST")]
public async Task<HttpResponseMessage> HttpQueueTest(HttpRequestMessage req, ICloudStorage storage)
{
	var id = Guid.NewGuid().ToString();

	var tcs = new TaskCompletionSource<int>();

	lock (Pending)
	{
		Pending.Add(id, tcs);
	}

	await storage.AddMessageAsync("acceptance-test-queue", new QueueMessage { Id = id });

	var task = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
	if (task != tcs.Task)
	{
		return req.CreateResponse(HttpStatusCode.GatewayTimeout);
	}

	return req.CreateResponse();
}

[QueueTrigger("acceptance-test-queue")]
public void QueueTest(QueueMessage message)
{
	lock (Pending)
	{
		if (Pending.TryGetValue(message.Id, out var tcs))
		{
			tcs.SetResult(0);
		}
	}
}

// ====

[HttpTrigger("POST")]
public async Task<HttpResponseMessage> HttpBlobTest(HttpRequestMessage req, ICloudStorage storage)
{
	var id = Guid.NewGuid().ToString();

	var tcs = new TaskCompletionSource<int>();

	lock (Pending)
	{
		Pending.Add(id, tcs);
	}

	await storage.UploadFromStreamAsync("acceptance-test-container", id, await req.Content.ReadAsStreamAsync());

	var task = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
	if (task != tcs.Task)
	{
		return req.CreateResponse(HttpStatusCode.GatewayTimeout);
	}

	return req.CreateResponse();
}

[BlobTrigger("acceptance-test-container")]
public void BlobTest(CloudBlockBlob blob)
{
	var target = new byte[4];
	blob.DownloadToByteArray(target, 0);
	if (target[0] == 1 && target[1] == 2 && target[2] == 3 && target[3] == 4)
	{
		lock (Pending)
		{
			if (Pending.TryGetValue(blob.Name, out var tcs))
			{
				tcs.SetResult(0);
			}
		}
	}
}

// ====

// todo: conditional variable
static object LockObject = new object();

[HttpTrigger]
public async Task<HttpResponseMessage> HttpTimerTest(HttpRequestMessage req)
{
	var tcs = new TaskCompletionSource<int>();

	ThreadPool.QueueUserWorkItem(_ =>
	{
		lock (LockObject)
		{
			if (Monitor.Wait(LockObject, TimeSpan.FromSeconds(30)))
			{
				tcs.SetResult(0);
			}
			else
			{
				tcs.SetCanceled();
			}
		}
	});

	await tcs.Task;

	return req.CreateResponse();
}

[TimerTrigger("*/5 * * * * *")] // every 5 second
public void Timer()
{
	lock (LockObject) {
		Monitor.PulseAll(LockObject);
	}
}