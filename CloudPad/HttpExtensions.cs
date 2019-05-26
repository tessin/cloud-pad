using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace CloudPad {
  public static class HttpExtensions {
    public static T GetRouteDataValue<T>(this HttpRequestMessage req, string name, T defaultValue = default(T)) {
      if (req.Properties.TryGetValue("MS_AzureWebJobs_HttpRouteData", out var routeData)) {
        var routeData2 = routeData as Dictionary<string, object>;
        if (routeData2 != null) {
          if (routeData2.TryGetValue(name, out var value)) {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
          }
        }
      }
      return defaultValue;
    }
  }
}
