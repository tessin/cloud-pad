<Query Kind="Program">
  <NuGetReference Version="9.0.1">Newtonsoft.Json</NuGetReference>
  <Namespace>System.Net</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
	var http = new Http { BaseUrl = new Uri("http://localhost:7071") };

	Test.Run("HttpTest", t => t.AssertEquals("Hello World!", http.GetJson<string>("/api/acceptance-test_HttpTest")));

	Test.Run("HttpRouteValueTest", t => t.AssertEquals("Hello LINQPad!", http.GetJson<string>("/api/hello/LINQPad")));

	Test.Run("HttpQueryValueTest", t => t.AssertEquals("Query Parameter!", http.GetJson<string>("/api/acceptance-test_HttpQueryValueTest?name=Parameter")));

	Test.Run("HttpQueueTest", t => http.DoPost("/api/acceptance-test_HttpQueueTest"));

	Test.Run("HttpBlobTest", t => http.DoFile("/api/acceptance-test_HttpBlobTest", new byte[] { 1, 2, 3, 4 }));

	Test.Run("HttpTimerTest", t => http.DoGet("/api/acceptance-test_HttpTimerTest"));
}

// Define other methods and classes here

class Http
{
	public Uri BaseUrl { get; set; }

	public HttpWebRequest CreateRequest(string method, string requestUrl)
	{
		var req = WebRequest.CreateHttp(new Uri(BaseUrl, requestUrl));
		req.Method = method;
		//		req.Headers.Set(HttpRequestHeader.Authorization, _auth);
		req.Accept = "application/json";
		return req;
	}

	public HttpWebResponse Do(HttpWebRequest req)
	{
		try
		{
			return (HttpWebResponse)req.GetResponse();
		}
		catch (WebException we)
		{
			var resp = we.Response as HttpWebResponse;
			if (resp == null)
				throw;
			return resp;
		}
	}

	public HttpWebResponse DoJson(HttpWebRequest req, object payload)
	{
		req.ContentType = "application/json";
		using (var reqStream = req.GetRequestStream())
		{
			var text = JsonConvert.SerializeObject(payload);
			var bytes = Encoding.UTF8.GetBytes(text);
			reqStream.Write(bytes, 0, bytes.Length);
		}
		return Do(req);
	}

	public void Check(HttpWebResponse res)
	{
		if (!(200 <= (int)res.StatusCode && (int)res.StatusCode <= 299))
		{
			// new StreamReader(res.GetResponseStream()).ReadToEnd()

			throw new InvalidOperationException($"Request '{res.ResponseUri}' unexpected response status {(int)res.StatusCode} {res.StatusDescription}");
		}
	}

	public T CheckJson<T>(HttpWebResponse res)
	{
		Check(res);

		using (var reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8, false))
		{
			return JsonConvert.DeserializeObject<T>(reader.ReadToEnd().Dump());
		}
	}
}

static class HttpExtensions
{
	public static void DoGet(this Http http, string requestUrl)
	{
		var req = http.CreateRequest("GET", requestUrl);
		using (var res = http.Do(req))
		{
			http.Check(res);
		}
	}

	public static void DoPost(this Http http, string requestUrl)
	{
		var req = http.CreateRequest("POST", requestUrl);
		req.ContentLength = 0; // required
		using (var res = http.Do(req))
		{
			http.Check(res);
		}
	}

	public static void DoFile(this Http http, string requestUrl, byte[] bytes)
	{
		var req = http.CreateRequest("POST", requestUrl);
		using (var reqStream = req.GetRequestStream())
		{
			reqStream.Write(bytes, 0, bytes.Length);
		}
		using (var res = http.Do(req))
		{
			http.Check(res);
		}
	}


	public static T GetJson<T>(this Http http, string requestUrl)
	{
		var req = http.CreateRequest("GET", requestUrl);
		using (var res = http.Do(req))
		{
			return http.CheckJson<T>(res);
		}
	}
}

class Test
{
	public static void Run(string name, Action<Test> test)
	{
		var context = new Test { Name = name };
		try
		{
			test(context);

			Util.RawHtml(new XElement("span", new XAttribute("style", "color:green"), $"Test '{name}' (ok)")).Dump();
		}
		catch (TestAssertionFailure ex)
		{
			Util.RawHtml(new XElement("span", new XAttribute("style", "color:red"), $"Test '{name}' (fail: {ex.Message})")).Dump();
		}
		catch (Exception ex)
		{
			Util.RawHtml(new XElement("span", new XAttribute("style", "color:red"), $"Test '{name}' (error: {ex.Message})")).Dump();
		}
	}

	// ========

	public string Name { get; set; }

	public T AssertEquals<T>(T expected, T actual)
	{
		if (!object.Equals(expected, actual))
		{
			throw new TestAssertionFailure($"expected <{expected}> actual <{actual}>");
		}
		return actual;
	}
}

class TestAssertionFailure : Exception
{
	public TestAssertionFailure(string message) : base(message)
	{
	}
}
