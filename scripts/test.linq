<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <NuGetReference Version="5.2.3">Microsoft.AspNet.WebApi.Core</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld")]
public Task<HttpResponseMessage> HelloWorld(HttpRequestMessage req, CancellationToken cancellationToken)
{
	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StringContent("Hello World!", Encoding.UTF8, "text/plain");
	return Task.FromResult(res);
}

