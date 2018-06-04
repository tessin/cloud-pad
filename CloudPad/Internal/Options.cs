namespace CloudPad.Internal
{
    class Options
    {
        /// <summary>
        /// LINQPad script file name is typically not set, instead it's inferred from the LINQPad query context
        /// </summary>
        public const string Script = "script";

        public const string Method = "method";

        public const string Compile = "compile";

        public const string OutputDirectory = "output";

        public const string Debug = "debug";

        public const string Install = "install";

        // mutually exclusive, implies compile
        public const string Publish = "publish";
        public const string Unpublish = "unpublish";

        public const string RequestFileName = "req";
        public const string ResponseFileName = "res";
    }
}
