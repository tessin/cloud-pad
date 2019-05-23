<Query Kind="Program">
  <Reference Relative="CloudPad\bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.FileSystem.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.dll</Reference>
  <Namespace>CloudPad.Internal</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

void Main(string[] args)
{
	if (!Directory.Exists(args[0] ?? ""))
	{
		$"Directory '{args[0]}' does not exist.".Dump();
		return;
	}

	var publishSettingsFileName = FileUtil.ResolveSearchPatternUpDirectoryTree(Environment.CurrentDirectory, "*.PublishSettings").Single();

	var kudu = KuduClient.FromPublishProfile(publishSettingsFileName);

	if (Util.ReadLine($"publish to '{kudu.Host}' [y/n]?") != "y")
	{
		return;
	}

	kudu.ZipDeploy(args[0]);

	"Done.".Dump();
}

// Define other methods and classes here
