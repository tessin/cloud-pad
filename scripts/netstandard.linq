<Query Kind="Program">
  <Reference Relative="..\CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Azure.KeyVault.Core.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.Edm.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.OData.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.Data.Services.Client.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Microsoft.WindowsAzure.Storage.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Spatial.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Spatial.dll</Reference>
  <Reference Relative="..\CloudPad\bin\Debug\net461\System.Web.Http.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\System.Web.Http.dll</Reference>
  <NuGetReference Version="1.1.0">Tessin.XamlBitmapClient</NuGetReference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tessin</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

Task Main(string[] args) => Program.MainAsync(this, args);
//Task Main(string[] args) => Program.MainAsync(this, new[] { "--compile" });

[HttpTrigger(AuthorizationLevel.Anonymous, "get")]
public void X(HttpRequestMessage req)
{
	new Tessin.XamlBitmapClient("");
}

// Define other methods and classes here