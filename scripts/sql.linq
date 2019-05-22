<Query Kind="Program">
  <Connection>
    <ID>b35a37c4-211c-4d06-83a4-4e6941102efe</ID>
    <Server>(localdb)\MSSQLLocalDB</Server>
    <Database>AzureStorageEmulatorDb59</Database>
    <ShowServer>true</ShowServer>
  </Connection>
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <NuGetReference Version="5.2.3">Microsoft.AspNet.WebApi.Core</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

//Task Main(string[] args) => Program.MainAsync(this, args);
Task Main(string[] args) => Program.MainAsync(this, new[] {"--compile", "--out-dir", @"C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad.FunctionApp\bin\Debug\net461"});

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld")]
public Task<HttpResponseMessage> HelloWorld(HttpRequestMessage req, CancellationToken cancellationToken)
{
	var res = req.CreateResponse(HttpStatusCode.OK);
	res.Content = new StringContent(Util.ToHtmlString(Accounts), Encoding.UTF8, "text/html");
	return Task.FromResult(res);
}