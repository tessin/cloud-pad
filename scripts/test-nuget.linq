<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad3\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationFramework.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Xaml.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\WindowsBase.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\UIAutomationProvider.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\UIAutomationTypes.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\ReachFramework.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationUI.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\System.Printing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Accessibility.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Deployment.dll</Reference>
  <NuGetReference Version="5.2.3">Microsoft.AspNet.WebApi.Core</NuGetReference>
  <NuGetReference>Tessin.XamlBitmapClient</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tessin</Namespace>
  <Namespace>System.Windows.Controls</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

//Task Main(string[] args) => Program.MainAsync(this, new[] { "--compile", "--compile-out-dir", @"C:\Users\leidegre\Source\tessin\cloud-pad3\CloudPad.FunctionApp\bin\Debug\net461" });
Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

FileDependency html = new FileDependency("test.html");

[HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "HelloWorld2")]
public async Task<HttpResponseMessage> HelloWorld(HttpRequestMessage req, CancellationToken cancellationToken, ILogger log)
{
	var xb = new XamlBitmapClient(service);
	await xb.InitializeAsync();
	try
	{
		var result = await xb.RenderAsync(new XamlBitmapRenderRequest()
		{
			PackageUri = new Uri(package),
			ViewTypeName = "Partner.PartnerUserControl, Partner",
			ViewModelTypeName = "Partner.PartnerViewModel, Partner",
			ViewModel = JObject.Parse("{\"BannerWidth\":848,\"BannerHeight\":300,\"BannerTitle\":\"Aktuella fastighetsprojekt på Tessin.se\",\"ImageHeight\":0,\"ProgressWidth\":70,\"ProgressMargin\":10,\"HeaderFont\":\"pack://application:,,,./Fonts/#Roboto REGULAR\",\"HeaderFontSize\":20,\"HeaderPadding\":\"10\",\"HeaderBackgroundColor\":\"#cccccc\",\"FooterFont\":\"pack://application:,,,./Fonts/#Roboto REGULAR\",\"FooterFontSize\":15,\"FooterHeight\":60,\"ArcColor\":\"#36b89a\",\"Projects\":[{\"Title\":\"Centralt belägen hyresfastighet i Umeå\",\"Progress\":50,\"ImageUrl\":\"http://files.tessin.se/projects/uthyrd-fastighet-umea/hero_780x400.jpg\",\"ArcAngle\":180.0,\"ProgressFormatted\":\"50\"},{\"Title\":\"Byggstartade bostäder i centrala Skellefteå\",\"Progress\":100,\"ImageUrl\":\"http://files.tessin.se/projects/byggstartat-bostadsprojekt-skelleftea/hero_780x400.jpg\",\"ArcAngle\":360.0,\"ProgressFormatted\":\"100\"},{\"Title\":\"Försålda kedjehus vid Mälaren\",\"Progress\":110,\"ImageUrl\":\"http://files.tessin.se/projects/nyproducerade-kedjehus-strangnas/hero_780x400.jpg\",\"ArcAngle\":360.0,\"ProgressFormatted\":\"110\"}]}"),
			Targets = {
				new XamlBitmapRenderTarget { Width = 640, Height = 480 }
			}
		});

		if (result.ErrorCode != 0)
		{
			var res = req.CreateResponse(HttpStatusCode.InternalServerError);
			res.Content = new StringContent(Util.ToHtmlString(result), Encoding.UTF8, "text/html");
			return res;
		}
		else
		{
			var res = req.CreateResponse(HttpStatusCode.Found);
			res.Headers.Location = result.Targets[0].DestinationUri;
			return res;
		}
	}
	finally
	{
		await xb.CloseAsync();
	}
}

const string service = "";
const string package = "";
