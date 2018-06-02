using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace CloudPad.Internal
{
    // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook#trigger---configuration

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BindingType
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "httpTrigger")]
        HttpTrigger,
        [EnumMember(Value = "timerTrigger")]
        TimerTrigger
    }

    public class Binding
    {
        public static Binding GetBinding(MethodInfo m)
        {
            var httpTrigger = m.GetCustomAttribute<HttpTriggerAttribute>();
            if (httpTrigger != null)
            {
                var binding = new Binding(BindingType.HttpTrigger)
                {
                    AuthLevel = httpTrigger.AuthLevel,
                    Methods = httpTrigger.Methods,
                    Route = httpTrigger.Route ?? m.Name
                };
                return binding;
            }

            var timerTrigger = m.GetCustomAttribute<TimerTriggerAttribute>();
            if (timerTrigger != null)
            {
                return new Binding(BindingType.TimerTrigger)
                {
                    CronExpression = timerTrigger.CronExpression,
                    RunAtStartup = timerTrigger.RunAtStartup,
                };
            }

            return null;
        }

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

        [JsonProperty("authLevel", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AuthorizationLevel? AuthLevel { get; private set; }

        [JsonProperty("methods", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Methods { get; private set; }

        [JsonProperty("route", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Route { get; private set; }

        [JsonProperty("schedule", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CronExpression { get; private set; }

        [JsonIgnore]
        public bool? RunAtStartup { get; private set; }

        public Binding(BindingType type)
        {
            Type = type;
        }
    }
}
