<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Net.Http.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Algorithms.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Security.Cryptography.Algorithms.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Encoding.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Security.Cryptography.Encoding.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.Primitives.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Security.Cryptography.Primitives.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Security.Cryptography.X509Certificates.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Security.Cryptography.X509Certificates.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Spatial.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Spatial.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Web.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Web.Http.dll</Reference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Queue</Namespace>
</Query>

const string QueueName = "cloud-pad-queue";

Task Main(string[] args) => Program.MainAsync(this, args);

public class Message
{
	public string Text { get; set; }
}

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld")] // trigger binds to first parameter, here it's a HttpRequestMessage because it is a HTTP trigger
public async Task HelloWorld(HttpRequestMessage req, CloudStorageHelper storage)
{
	// Http triggers don't have to return a message. 
	// If they don't they will respond with a "204 No Content" if no error is thrown.

	await storage.AddQueueMessageAsync(QueueName, new Message { Text = "Hello World!" });
}

 // again, trigger binds to first parameter, here it's a user defined Message type because it is a queue trigger (the message will be deserialized from JSON)
[QueueTrigger(QueueName)]
public void HelloWorldQueueMessage(Message msg)
{
	msg.Dump();
}