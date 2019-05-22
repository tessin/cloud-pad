using CloudPad.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;

namespace CloudPad {
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class HttpTriggerAttribute : Attribute, ITriggerAttribute {
    /// <summary>
    /// Constructs a new instance.
    /// </summary>
    public HttpTriggerAttribute() {
      AuthLevel = AuthorizationLevel.Function;
    }

    /// <summary>
    /// Constructs a new instance.
    /// </summary>        
    /// <param name="methods">The http methods to allow.</param>
    public HttpTriggerAttribute(params string[] methods) : this() {
      Methods = methods;
    }

    /// <summary>
    /// Constructs a new instance.
    /// </summary>
    /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
    public HttpTriggerAttribute(AuthorizationLevel authLevel) {
      AuthLevel = authLevel;
    }

    /// <summary>
    /// Constructs a new instance.
    /// </summary>
    /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
    /// <param name="methods">The http methods to allow.</param>
    public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods) {
      AuthLevel = authLevel;
      Methods = methods;
    }

    /// <summary>
    /// Gets or sets the route template for the function. Can include
    /// route parameters using WebApi supported syntax. If not specified,
    /// will default to the function name.
    /// </summary>
    public string Route { get; set; }

    /// <summary>
    /// Gets the http methods that are supported for the function.
    /// </summary>
    public string[] Methods { get; set; }

    /// <summary>
    /// Gets the authorization level for the function.
    /// </summary>
    public AuthorizationLevel? AuthLevel { get; set; }

    // ====

    Type[] ITriggerAttribute.GetRequiredParameterTypes() {
      return new[] { typeof(HttpRequestMessage) };
    }

    object ITriggerAttribute.GetBindings() {
      // The documentation is here but it's incomplete
      // see https://github.com/Azure/azure-functions-host/wiki/function.json

      var bindings = new JArray();

      var httpTrigger = new JObject();

      httpTrigger["type"] = "httpTrigger";
      httpTrigger["direction"] = "in";
      httpTrigger["name"] = "req";

      if (Methods != null) {
        httpTrigger["methods"] = JToken.FromObject(Methods.Select(m => m.ToLowerInvariant()));
      }

      if (Route != null) {
        httpTrigger["route"] = Route;
      }

      if (AuthLevel != null) {
        httpTrigger["authLevel"] = JToken.FromObject(AuthLevel.Value);
      }

      bindings.Add(httpTrigger);

      // ==== 

      var http = new JObject();

      http["type"] = "http";
      http["direction"] = "out";
      http["name"] = "$return";

      bindings.Add(http);

      return bindings;
    }

    string ITriggerAttribute.GetEntryPoint() {
      return "CloudPad.FunctionApp.HttpTrigger.Run";
    }
  }
}
