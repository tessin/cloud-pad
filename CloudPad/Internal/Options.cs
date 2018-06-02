namespace CloudPad.Internal
{
    class Options
    {
        public const string Method = "method";

        public const string Compile = "compile";

        public const string OutputDirectory = "out";

        // mutually exclusive, implies compile
        public const string Publish = "publish";
        public const string Unpublish = "unpublish";

        public const string RequestFileName = "req";
        public const string ResponseFileName = "res";
    }
}
