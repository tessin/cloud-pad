using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace CloudPad {
  public static partial class CloudPadExtensions {
    public static T GetRouteValue<T>(this HttpRequestMessage req, string name, T defaultValue = default(T)) {
      if (req.Properties.TryGetValue("MS_AzureWebJobs_HttpRouteData", out var routeData)) { // Azure Functions shenanigans
        var routeData2 = routeData as Dictionary<string, object>;
        if (routeData2 != null) {
          if (routeData2.TryGetValue(name, out var value)) {
            try {
              return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            } catch (Exception ex) {
              throw new ArgumentException($"Request route parameter '{name}' is invalid", name, ex);
            }
          }
        }
      }
      return defaultValue;
    }

    public static T GetQueryValue<T>(this HttpRequestMessage req, string name, T defaultValue = default(T), bool isRequired = false) {
      foreach (var item in req.GetQueryNameValuePairs()) {
        if (string.Equals(name, item.Key, StringComparison.OrdinalIgnoreCase)) {
          try {
            return (T)Convert.ChangeType(item.Value, typeof(T), CultureInfo.InvariantCulture);
          } catch (Exception ex) {
            throw new ArgumentException($"Request query string parameter '{name}' with value '{item.Value}' is invalid. " + ex.Message, name, ex);
          }
        }
      }
      if (isRequired) {
        throw new ArgumentException($"Request query string parameter '{name}' is required");
      }
      return defaultValue;
    }

    public static List<T> GetQueryValues<T>(this HttpRequestMessage req, string name) {
      var values = new List<T>();
      foreach (var item in req.GetQueryNameValuePairs()) {
        if (string.Equals(name, item.Key, StringComparison.OrdinalIgnoreCase)) {
          try {
            values.Add((T)Convert.ChangeType(item.Value, typeof(T), CultureInfo.InvariantCulture));
          } catch (Exception ex) {
            throw new ArgumentException($"Request query string parameter '{name}' with value '{item.Value}' is invalid. " + ex.Message, name, ex);
          }
        }
      }
      return values; // default
    }
  }
}
