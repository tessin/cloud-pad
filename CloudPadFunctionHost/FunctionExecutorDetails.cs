using Newtonsoft.Json;

namespace CloudPadFunctionHost
{
    class FunctionExecutorDetails
    {
        [JsonProperty("linqPadScriptFileName")]
        public string LINQPadScriptFileName { get; set; }

        [JsonProperty("linqPadScriptMethodName")]
        public string LINQPadScriptMethodName { get; set; }
    }
}
