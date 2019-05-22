namespace CloudPad.Internal {
  interface IFunctionMetadata {
    string ProviderName { get; }
    string ConnectionString { get; set; }
  }
}
