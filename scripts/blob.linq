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
  <Namespace>Microsoft.WindowsAzure.Storage.Blob</Namespace>
</Query>

const string BlobContainer = "cloud-pad-blob-container";

Task Main(string[] args) => Program.MainAsync(this, args);

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST", Route = "Upload")]
public async Task<HttpResponseMessage> HelloBlob(HttpRequestMessage req, CancellationToken cancellationToken, ITraceWriter log, ICloudStorage storage)
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