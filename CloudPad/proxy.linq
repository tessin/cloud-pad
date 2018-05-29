<Query Kind="Program">
  <Reference Relative="bin\Debug\net461\CloudPad.dll">C:\Users\leidegre\Source\tessin\CloudPad\CloudPad\bin\Debug\net461\CloudPad.dll</Reference>
  <Namespace>LINQPad.ObjectModel</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	var cts = new CancellationTokenSource();
	Util.Cleanup += (sender, e) => cts.Cancel();
	using (var proxy = new CloudPad.Internal.Proxy(CompileAsync))
	{
		await proxy.RunAsync(args, cts.Token);
	}
}

// Define other methods and classes here

static System.Collections.Concurrent.ConcurrentDictionary<string, LINQPadScript> _cache = new System.Collections.Concurrent.ConcurrentDictionary<string, LINQPadScript>();

static async Task<CloudPad.Internal.ILINQPadScript> CompileAsync(string linqPadScriptFileName)
{
_Compile:
	if (!_cache.TryGetValue(linqPadScriptFileName, out var compilation))
	{
		if (!_cache.TryAdd(linqPadScriptFileName, compilation = new LINQPadScript(await Util.CompileAsync(linqPadScriptFileName))))
		{
			// discard
			compilation.Dispose();
			goto _Compile;
		}
	}
	return compilation;
}

class LINQPadScript : CloudPad.Internal.ILINQPadScript, IDisposable
{
	private readonly LINQPad.ObjectModel.QueryCompilation _compilation;

	public LINQPadScript(LINQPad.ObjectModel.QueryCompilation compilation)
	{
		_compilation = compilation;
	}

	public async Task<CloudPad.Internal.ILINQPadScriptResult> RunAsync(string[] args)
	{
		var executor = _compilation.Run(QueryResultFormat.Text, args);
		await executor.WaitAsync();
		return new LINQPadScriptResult(executor);
	}

	public void Dispose()
	{
		_compilation.Dispose();
	}
}

class LINQPadScriptResult : CloudPad.Internal.ILINQPadScriptResult
{
	private readonly QueryExecuter _executor;

	public LINQPadScriptResult(QueryExecuter executor)
	{
		this._executor = executor;
	}

	public Task<string> GetResultAsync()
	{
		return this._executor.AsStringAsync();
	}
}