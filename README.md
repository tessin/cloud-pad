# CloudPad<br/><small>Run LINQPad scripts as Azure functions</small>

## Getting started

Add reference to NuGet package `CloudPad` (_if you don't have a LINQPad license you can add a reference to the lib directly_).

There's minimal setup but you cannot just take any LINQPad script and run it as an Azure function you need to have this bootstrapping snippet in your `Main` method. It's important to pass in both `this` and `args` to the `CloudPadJobHost` constructor and call `WaitAsync` for the LINQPad script to function when deployed.

Once deployed, any non-static method defined in the LINQPad script is potentially an Azure function that can be invoked. As long as there is a supported binding everything should just work!

As of writing, supported bindings are:

* HTTP
* Timer

```cs
async Task Main(string[] args)
{
  using (var host = new CloudPadJobHost(this, args))
  {
    await host.WaitAsync();
  }
}

// Define other methods and classes here

[Route("hello")] // System.Web.Http (todo: replace with HttpTrigger)
HttpResponseMessage Hello(HttpRequestMessage req)
{
  return req.CreateText("world");
}
```
