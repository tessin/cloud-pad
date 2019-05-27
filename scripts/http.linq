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
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "Hello/{name}")]
public Task<HttpResponseMessage> Hello(HttpRequestMessage req, CancellationToken cancellationToken, ILogger log)
{
	// the actual route data value is not found here, this is diffrent from ASP.NET Web API
	req.GetRouteData().Values.Dump();

	// but here (Azure Function Runtime shenanigans), GetRouteValue is an extension method provided by CloudPad
	var name = req.GetRouteValue("name", "");
	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StringContent($"Hello {name}!", Encoding.UTF8, "text/plain");
	return Task.FromResult(res);
}

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "Hello")]
public Task<HttpResponseMessage> HelloWithQuery(HttpRequestMessage req, CancellationToken cancellationToken, ILogger log)
{
	// you can do this, or...
	req.GetQueryNameValuePairs().Dump();

	// you can do this, GetQueryValue is an extension method provided by CloudPad very similar to GetRouteValue
	var name = req.GetQueryValue("name", "", isRequired: true);

	// note that, by default query string parameters are not required and support mutliple values, for example
	req.GetQueryValues<string>("name").Dump();
	
	// in the case of mutliple values, `GetQueryValue` always gets the first value
	// GetQueryValue and GetQueryValues are not case sensitive

	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StringContent($"Hello {name}!", Encoding.UTF8, "text/plain");
	return Task.FromResult(res);
}