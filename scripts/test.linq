<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad3\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <NuGetReference Version="5.2.3">Microsoft.AspNet.WebApi.Core</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);
//Task Main(string[] args) => Program.MainAsync(this, new[] { "--compile", "--compile-out-dir", @"C:\Users\leidegre\Source\tessin\cloud-pad3\CloudPad.FunctionApp\bin\Debug\net461" });

// Define other methods and classes here

FileDependency html = new FileDependency("test.html");

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld")]
public Task<HttpResponseMessage> HelloWorld(HttpRequestMessage req, CancellationToken cancellationToken)
{
	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StreamContent(html.OpenRead());
	res.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
	return Task.FromResult(res);
}


[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld2")]
public Task<HttpResponseMessage> HelloWorld2(HttpRequestMessage req, CancellationToken cancellationToken)
{
	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StringContent("Hello World 2!", Encoding.UTF8, "text/plain");
	return Task.FromResult(res);
}