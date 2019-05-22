namespace CloudPad.Internal {
  class QueryInfo {
    public string Provider { get; }

    public QueryInfo(object query) {
      var queryType = query.GetType();
      var getConnectionInfo = queryType.GetMethod("GetConnectionInfo");
      var repo = getConnectionInfo.Invoke(query, null);
      if (repo == null) {
        // ok, no connection
      } else {
        var repoType = repo.GetType();
        var databaseInfo = repoType.GetProperty("DatabaseInfo").GetValue(repo);
        var databaseInfoType = databaseInfo.GetType();

        Provider = (string)databaseInfoType.GetProperty("Provider").GetValue(databaseInfo);
      }
    }
  }
}
