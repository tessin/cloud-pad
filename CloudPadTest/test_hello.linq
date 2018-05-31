<Query Kind="Program">
  <Reference Relative="bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\cloud-pad\CloudPadTest\bin\Debug\net461\CloudPad.dll</Reference>
  <Namespace>CloudPad</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	using (var host = new CloudPadJobHost(this, args))
	{
		await host.WaitAsync();
	}
}

// Define other methods and classes here

[TimerTrigger("")]
void Hello()
{
	// no op
}