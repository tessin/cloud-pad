<Query Kind="Program">
  <NuGetReference>Octokit</NuGetReference>
  <Namespace>Octokit</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var client = new GitHubClient(new Octokit.ProductHeaderValue("octokit"));
	var repository = await client.Repository.Get("tessin", "AzureLINQPadFunctions");
	repository.Dump();
}
