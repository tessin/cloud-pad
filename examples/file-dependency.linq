<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Release\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Release\net461\CloudPad.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Release\net461\NCrontab.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Release\net461\NCrontab.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Release\net461\Newtonsoft.Json.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Release\net461\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Release\net461\System.Net.Http.Formatting.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Release\net461\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Release\net461\System.Web.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPad\bin\Release\net461\System.Web.Http.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Web.Http</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

FileDependency html = new FileDependency("hello.html");

[HttpTrigger(Route = "test")]
HttpResponseMessage TestHttp(HttpRequestMessage req)
{
	var res = req.CreateResponse();
	res.Content = new StreamContent(html.OpenRead()) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html") } };
	return res;
}
