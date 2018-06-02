<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Web.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Debug\net461\System.Web.Http.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

[HttpTrigger(Route = "test")]
HttpResponseMessage Test(HttpRequestMessage req)
{
	var res = req.CreateResponse();
	res.Content = new StringContent("hello world", Encoding.UTF8, "text/plain");
	return res;
}

[HttpTrigger(Route = "test-async")]
async Task<HttpResponseMessage> TestAsync(HttpRequestMessage req, CancellationToken cancellationToken)
{
	await Task.Delay(100);

	var res = req.CreateResponse();
	res.Content = new StringContent("hello world asynchronous", Encoding.UTF8, "text/plain");
	return res;
}