using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CloudPad.Internal
{
    // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook#trigger---configuration

    [JsonConverter(typeof(StringEnumConverter))]
    enum BindingType
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "httpTrigger")]
        HttpTrigger,
        [EnumMember(Value = "timerTrigger")]
        TimerTrigger
    }

    class Binding
    {
        private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [JsonProperty("type")]
        public BindingType Type { get; }

        [JsonProperty("direction")]
        public string Direction => "in";

        [JsonProperty("name")]
        public string Name
        {
            get
            {
                switch (Type)
                {
                    case BindingType.HttpTrigger: return "req";
                    case BindingType.TimerTrigger: return "myTimer";
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        [JsonIgnore]
        public MethodInfo Method { get; }

        [JsonProperty("authLevel", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AuthorizationLevel? AuthLevel { get; private set; }

        [JsonProperty("methods", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Methods { get; private set; }

        [JsonProperty("route", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Route { get; private set; }

        [JsonProperty("schedule", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CronExpression { get; private set; }

        public Binding(BindingType type, MethodInfo method)
        {
            Type = type;
            Method = method;
        }

        public static Binding GetBinding(Type contextType, string methodName)
        {
            var m = contextType.GetMethod(methodName, _bindingFlags);
            if (m == null)
            {
                return null;
            }
            return GetBinding(m);
        }

        public static Binding GetBinding(MethodInfo m)
        {
            var httpTrigger = m.GetCustomAttribute<HttpTriggerAttribute>(); // todo: replace with HttpTrigger
            if (httpTrigger != null)
            {
                var binding = new Binding(BindingType.HttpTrigger, m)
                {
                    AuthLevel = httpTrigger.AuthLevel,
                    Methods = httpTrigger.Methods,
                    Route = httpTrigger.Route ?? m.Name // todo: m.Name can be overriden by function name?
                };
                return binding;
            }

            var timerTrigger = m.GetCustomAttribute<TimerTriggerAttribute>();
            if (timerTrigger != null)
            {
                return new Binding(BindingType.TimerTrigger, m)
                {
                    CronExpression = timerTrigger.CronExpression
                };
            }

            return null;
        }

        public string GetMethodName()
        {
            return Method.Name;
        }
    }
}
