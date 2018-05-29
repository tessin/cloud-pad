<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\CloudPad\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <NuGetReference Version="5.2.3">Microsoft.AspNet.WebApi.Core</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Web.Http</Namespace>
</Query>

async Task Main(string[] args)
{
	var cts = new CancellationTokenSource();
	Util.Cleanup += (sender, e) => cts.Cancel();
	using (var host = new CloudPadJobHost(this, args))
	{
		await host.WaitAsync(cts.Token);
	}
}

// Define other methods and classes here

[Route("api/hello")]
HttpResponseMessage Hello(HttpRequestMessage req)
{
	return req.CreateText("world");
}