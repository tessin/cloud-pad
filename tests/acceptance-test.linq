<Query Kind="Program">
  <NuGetReference Version="2.0.0">CloudPad</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

[HttpTrigger(AuthorizationLevel.Anonymous)]
public HttpResponseMessage HttpTest(HttpRequestMessage req)
{
	return req.CreateResponse("Hello World!");
}
