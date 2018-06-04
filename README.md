# CloudPad

> 🌩️ Run LINQPad scripts as Azure functions

## Getting started

Add reference to NuGet package `CloudPad` (_if you don't have a LINQPad premium license you can add a reference to the assemblies directly. CloudPad does not require a premium license_).

There's minimal setup and you cannot just take any LINQPad script and run it as an Azure function you need to have this bootstrapping snippet in your LINQPad program.

```cs
Task Main(string[] args) => Program.MainAsync(this, args);
```

Any non-static method defined in the LINQPad script is _potentially_ an Azure function that can be invoked. As long as there is a supported binding, it should just work!

Supported bindings are:

* HTTP
* Timer

```cs
Task Main(string[] args) => Program.MainAsync(this, args);

// Define other methods and classes here

[HttpTrigger(Route = "test")]
HttpResponseMessage TestHttp(HttpRequestMessage req)
{
	var res = req.CreateResponse();
	res.Content = new StringContent("hello world", Encoding.UTF8, "text/plain");
	return res;
}

[HttpTrigger(AuthorizationLevel.Anonymous, Route = "test-async")]
async Task<HttpResponseMessage> TestHttpAsync(HttpRequestMessage req, CancellationToken cancellationToken)
{
	await Task.Delay(100);

	var res = req.CreateResponse();
	res.Content = new StringContent("hello world asynchronous", Encoding.UTF8, "text/plain");
	return res;
}

[TimerTrigger("0 */5 * * * *")] // every 5 minutes
async Task TestTimerAsync(CancellationToken cancellationToken)
{
	await Task.Delay(100);
}
```

The behavior should be identical to an Azure Function. If the behavior is different it is most likely a bug in `CloudPad`. Please open an issue to discuss the matter.

### HttpTrigger

By default, all HTTP triggers use the `Function` authorization level. That is, each HTTP endpoint requires a unique `code` query string parameter when deployed otherwise you will get a `401 Unauthorized` error. You can retrieve the `code` query string parameter from the Azure portal. This behavior can be changed using the `AuthLevel` property of the `HttpTrigger` attribute.

> Here, the example from above, where `AuthorizationLevel` has been changed to `Anonymous`:
> ```cs
> [HttpTrigger(AuthorizationLevel.Anonymous, Route = "test-async")]
> ```
> Note that this does not protect your HTTP endpoint. The endpoint is open to anyone and everyone.

### TimerTrigger

By default, all timer triggers run at startup. If this is undesirable there's a `CloudPad` specific property `RunAtStartup` (not to be confused with the Azure Web Jobs SDK setting `RunOnStartup`) that stops the timer trigger to running at startup of a local LINQPad script. Note that this setting has no effect once LINQPad script is deployed. If you want to disable the timer trigger locally for debugging purposes, simply remove the `TimerTrigger` attribute temporarily with a comment.

## Deployment

### Prepare the Azure Function App LINQPad script host

You can deploy several different LINQPad scripts to the same Azure Function App LINQPad script host but before you do so you need to prepare an **Azure Funciton App** environment.

You can find the latest `CloudPad.FunctionApp` release from the GitHub releases tab. Only the `CloudPad.FunctionApp` is released this way, otherwise the `CloudPad` NuGet package should be used. Sign in to the Azure portal and open the platform feature, Advanced tools (Kudu). Open a debug conole and goto the `D:\home\site\wwwroot` directory and unpack the zip file there. 

You also need to put a version of LINQPad on the Azure Funciton App environment. Download the LINQPad xcopy-deploy build from [here](http://www.linqpad.net/download.aspx) and unpack into `D:\home\site\tools\LINQPad.*`. Where `*` is the LINQPad version. As of writing this would be `5.31.0` so,  `D:\home\site\tools\LINQPad.5.31.0`. If successful you should have 4 files:

~~~
LINQPad.exe
LINQPad.exe.config
lprun.exe
lprun.exe.config
~~~

### Deploy LINQPad script

`CloudPad` is itself, its own little command-line tool but you need to run it through LINQPad, i.e. using `LPRun.exe`. As soon as you have your first script, you can _install_ `CloudPad` for ease of use, like this:

> `$ "C:\Program Files (x86)\LINQPad5\LPRun.exe" example.linq -install`

This will create a shortcut "Deploy LINQPad script to Azure" in the "Send To" Explorer file menu which in turn executes the following Batch file

```bat
@echo off
cd %~dp1
"C:\Program Files (x86)\LINQPad5\LPRun.exe" %1 -publish *.PublishSettings
```

The assumption here is that you have a `*.PublishSettings` file somewhere up the directory path that can be used to access an Azure function. For example, if you have a script in a folder `C:\Source\cloud-pad\example.linq` it will look for a publish profile `C:\Source\cloud-pad\*.PublishSettings` then `C:\Source\*.PublishSettings` and finally `C:\*.PublishSettings` before giving up. The publish profile can be found in the Azure portal, at the top menu bar for any App Service Web Site, including an Azure Function App.

## FAQ/Known issues

### `FileNotFoundException`, i.e. cannot load file or assembly error

When testing and developing you may get away with referencing only specific DLLs but when you deploy your script, the script can fail with file not found or assembly version mismatch errors. This is often due to missing explicit NuGet package or DLL references for the script itself. You should add additional NuGet package reference or DLLs if you run into this issue.

`CloudPad` is built for the `net461` target and depends on:

* `Microsoft.AspNet.WebApi.Core.5.2.3`
* `Newtonsoft.Json.9.0.1`.
* `ncrontab.3.3.0`.

If you attempt to add a NuGet package that is incompatible with these dependencies you may run into additional issues. If stuck on an issue like this, enable assembly binding logging, i.e. `FusionLog` to getter better and more detailed error information.

