We use the Azure Function runtime version 1.x. Because LINQPad requires it to work.

We support a subset of Azure Function bindings and LINQPad features.

The exact dependencies that we use are

```
<NuGetReference Version="2.3.0">Microsoft.Azure.WebJobs</NuGetReference>
<NuGetReference Version="2.3.0">Microsoft.Azure.WebJobs.Extensions</NuGetReference>
<NuGetReference Version="1.2.0">Microsoft.Azure.WebJobs.Extensions.Http</NuGetReference>
```

```
dotnet add . package -v 2.3.0 Microsoft.Azure.WebJobs
dotnet add . package -v 2.3.0 Microsoft.Azure.WebJobs.Extensions
dotnet add . package -v 1.2.0 Microsoft.Azure.WebJobs.Extensions.Http
```

```
dotnet add . package -v 5.2.3 Microsoft.AspNet.WebApi.Core
```

Due to the way the compilation process works you cannot have late bound (overly dynamic) assembly loading. The compilation step will try to forcefully load all referenced assemblies based on available metadata. If you load assemblies some other way they will most likely not be included automatically in the final package.

If the azure storage emulator is not running you may experience long pauses (timeouts) at start.

https://github.com/Azure/azure-webjobs-sdk/wiki/Application-Insights-Integration

# CloudPad.exe

We could build some command-line tooling into the CloudPad assembly. Possibly redistribute as an NPM package. Install as `yarn global add @tessin/cloud-pad@latest`

- `cpad create -g XYZ -n azfn-web-app`
- `cpad install -g XYZ -n azfn-web-app`
- `cpad upgrade -g XYZ -n azfn-web-app`

Under the hood, the above commands would both either/or create and deploy the CloudPad.FunctionApp (i.e. the CloudPad Azure Function Runtime). We would rely on the Azure CLI 2.0 tooling and Kudu Zip Deploy for this.

- `cpad publish script.linq`

Under the hood, the above command would locate LPRun, compile the script. Locate a publishing profile and publish the script. You can download the publishing profile from the Azure Portal. Place it any where up the directory tree.

# HTTP utilities

```cs
new Uri("http://localhost/").ParseQueryString()["asd"].Dump();
new Uri("http://localhost/?asd").ParseQueryString()["asd"].Dump();
new Uri("http://localhost/?asd=").ParseQueryString()["asd"].Dump();
new Uri("http://localhost/?asd=123").ParseQueryString()["asd"].Dump();
new Uri("http://localhost/?asd=123&asd=123").ParseQueryString()["asd"].Dump();
new Uri("http://localhost/?asd=123&asd=123").ParseQueryString().GetValues("asd").Dump();
```

Make sure you have the .NET Framework 4.7.2 Developer Pack installed.

https://docs.microsoft.com/en-us/azure/azure-functions/functions-versions

# Extending the Shortcut Menu for a File Type

https://docs.microsoft.com/en-us/windows/desktop/shell/context#extending-the-shortcut-menu-for-a-file-type

```
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\LINQPad\shell\publish]
@="Publish LINQPad script to Azure Function"
"Icon"="C:\\Program Files (x86)\\LINQPad5\\LINQPad.EXE,0"

[HKEY_CLASSES_ROOT\LINQPad\shell\publish\command]
@=""
```

# LINQPad Version

`5.36.03`

# CloudPad 3

CloudPad 3 will target .NET Core 3 and LINQPad 6

# azure-functions-core-tools

After much headache. Specifically around `System.Net.Http` the version that is shipping with the Azure Function Runtime 1.0.19 is from the .NET Framework 4.7.1. You need to provide assembly binding redirects to work around this issue.

I used a LINQPad script `func.linq` to rewrite the assembly reference metadata to be compatible with CloudPad. Then repackage the `Azure.Functions.Cli` tooling for this to work out of the box.
