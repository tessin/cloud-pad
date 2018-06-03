using Newtonsoft.Json;

namespace CloudPad
{
    class FunctionExecutorDetails
    {
        [JsonProperty("linqPadScriptFileName")]
        public string LINQPadScriptFileName { get; set; }

        [JsonProperty("linqPadScriptMethodName")]
        public string LINQPadScriptMethodName { get; set; }
    }
}
