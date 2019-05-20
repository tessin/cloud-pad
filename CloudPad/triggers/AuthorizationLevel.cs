using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace CloudPad
{
  [JsonConverter(typeof(StringEnumConverter))]
  public enum AuthorizationLevel
  {
    //
    // Summary:
    //     Allow access to anonymous requests.
    [EnumMember(Value = "anonymous")]
    Anonymous = 0,
    //
    // Summary:
    //     Allow access to requests that include a valid authentication token
    User = 1,
    //
    // Summary:
    //     Allow access to requests that include a function key
    [EnumMember(Value = "function")]
    Function = 2,
    //
    // Summary:
    //     Allows access to requests that include a system key
    System = 3,
    //
    // Summary:
    //     Allow access to requests that include the master key
    [EnumMember(Value = "admin")]
    Admin = 4
  }
}