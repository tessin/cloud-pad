using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace CloudPad.Internal
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ResultType
    {
        [EnumMember(Value = "SUCCESS")]
        None = 0,

        [EnumMember(Value = "ERROR_COMPILATION_FAILED")]
        CompileError,

        [EnumMember(Value = "ERROR_EXECUTION_FAILED")]
        RunError,
    }

    public class Result
    {
        public Guid CorrelationId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ResultType ErrorCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExceptionTypeFullName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExceptionMessage { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExceptionStackTrace { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExceptionFusionLog { get; set; }
    }
}
