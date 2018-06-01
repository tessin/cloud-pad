# 🌩️ CloudPad

> Run LINQPad scripts as Azure functions

## Getting started

Add reference to NuGet package `CloudPad` (_if you don't have a LINQPad premium license you can add a reference to the assemblies directly. CloudPad does not require a premium license_).

There's minimal setup and you cannot just take any LINQPad script and run it as an Azure function you need to have this bootstrapping snippet in your LINQPad program.

```cs
Task Main(string[] args) => CloudPad.CloudPad.MainAsync(this, args);
```

Any non-static method defined in the LINQPad script is _potentially_ an Azure function that can be invoked. As long as there is a supported binding, it should just work!

Supported bindings are:

* HTTP
* Timer

```cs
Task Main(string[] args) => CloudPad.CloudPad.MainAsync(this, args);

// Define other methods and classes here

[HttpTrigger(Route = "hello")]
HttpResponseMessage Hello(HttpRequestMessage req)
{
  var res = req.CreateResponse();
  res.Content = new StringContent(
    "world", Encoding.UTF8, "text/plain"
  );
  return res;
}
```

The behavior should be identical to an Azure Function. If the behavior is different it is most likely a bug in `CloudPad`. Please open an issue to discuss the matter.

## FAQ/Known issues

### `FileNotFoundException`, i.e. cannot load file or assembly error

When testing and developing you may get away with referencing only specific DLLs but when you deploy your script, the script can fail with file not found or assembly version mismatch errors. This is often due to missing explicit NuGet package or DLL references for the script itself. You should add additional NuGet package reference or DLLs if you run into this issue.

`CloudPad` depends on:

* `Microsoft.AspNet.WebApi.Core.5.2.3`
* `Newtonsoft.Json.9.0.1`.

If you attempt to add a NuGet package that is incompatible with these dependencies you may run into additional issues.

If stuck on an issue like this, enable assembly binding logging, i.e. `FusionLog` to getter better and more detailed error information.

## API Reference

TODO
