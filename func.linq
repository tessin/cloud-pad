<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.FileSystem.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.dll</Reference>
  <NuGetReference Version="0.10.3">Mono.Cecil</NuGetReference>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

readonly string rootDir = Path.GetDirectoryName(Util.CurrentQueryPath);

void Main()
{
	var d = GetAssemblyNameMappings(Path.Combine(rootDir, @"CloudPad\bin\Debug\net461"));

	var version = "1.0.19";
	var azureFunctionsCoreTools = GetAzureFunctionsCoreTools(version).TrimEnd('\\') + "\\";

	var funcRoot = Path.Combine(rootDir, "func");
	var funcDir = Path.Combine(funcRoot, version + "-net461");

	foreach (var path in Directory.EnumerateFiles(azureFunctionsCoreTools, "*.*", SearchOption.AllDirectories))
	{
		var rel = path.Substring(azureFunctionsCoreTools.Length);

		var dll = LoadAssemblyDefinition(path);
		if (dll != null)
		{
			var redirected = false;
			foreach (var r in dll.MainModule.AssemblyReferences)
			{
				if (d.TryGetValue(r.Name, out var redirect))
				{
					if (redirect.Version < r.Version)
					{
						r.Version = redirect.Version;
						redirected = true;
						$"Assembly '{dll.FullName}' reference to '{r.Name}' redirected to version '{redirect.Version}'".Dump();
					}
				}
			}
			if (redirected)
			{
				dll.Write(Path.Combine(funcDir, dll.MainModule.Assembly.Name.Name + Path.GetExtension(path)));
				continue;
			}
		}

		var dst = Path.Combine(funcDir, rel);
		Directory.CreateDirectory(Path.GetDirectoryName(dst));
		File.Copy(path, dst, true);
	}

	ZipFile.CreateFromDirectory(funcDir, Path.Combine(funcRoot, version + "-net461" + ".zip"));
}

// Define other methods and classes here

private string GetAzureFunctionsCoreTools(string version)
{
	var funcRoot = Path.Combine(rootDir, "func");
	var funcDir = Path.Combine(funcRoot, version + "-net471");
	var funcFileName = Path.Combine(funcDir, "func.exe");
	if (!File.Exists(funcFileName))
	{
		Directory.CreateDirectory(funcDir);
		var azureFunctionsCliZip = funcDir + ".zip";
		var req = WebRequest.Create($"https://functionscdn.azureedge.net/public/{version}/Azure.Functions.Cli.zip");
		using (var res = req.GetResponse())
		{
			using (var zip = File.Create(azureFunctionsCliZip))
			{
				res.GetResponseStream().CopyTo(zip);
			}
		}
		ZipFile.ExtractToDirectory(azureFunctionsCliZip, funcDir);
		File.Delete(azureFunctionsCliZip);
	}
	return funcDir;
}

private Dictionary<string, AssemblyNameDefinition> GetAssemblyNameMappings(string dir)
{
	var d = new Dictionary<string, AssemblyNameDefinition>();
	foreach (var path in Directory.EnumerateFiles(dir, "*.*"))
	{
		var dll = LoadAssemblyDefinition(path);
		if (dll != null)
		{
			d[dll.MainModule.Assembly.Name.Name] = dll.MainModule.Assembly.Name;
		}
	}
	return d;
}

private AssemblyDefinition LoadAssemblyDefinition(string path)
{
	var ext = Path.GetExtension(path);
	if (".dll".Equals(ext, StringComparison.OrdinalIgnoreCase) || ".exe".Equals(ext, StringComparison.OrdinalIgnoreCase))
	{
		try
		{
			return AssemblyDefinition.ReadAssembly(path);
		}
		catch
		{
			// nom nom nom...
		}
	}
	return null;
}