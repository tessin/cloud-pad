using CloudPad.Internal;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;

namespace CloudPad
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BlobTriggerAttribute : Attribute, ITriggerAttribute
    {
        public string BlobPath { get; }

        public BlobTriggerAttribute(string blobPath)
        {
            this.BlobPath = blobPath;
        }

        object ITriggerAttribute.GetBindings()
        {
            var bindings = new JArray();

            var blobTrigger = new JObject();

            blobTrigger["type"] = "blobTrigger";
            blobTrigger["direction"] = "in";
            blobTrigger["name"] = "blob";
            blobTrigger["path"] = BlobPath;

            bindings.Add(blobTrigger);

            return bindings;
        }

        string ITriggerAttribute.GetEntryPoint()
        {
            return "CloudPad.FunctionApp.BlobTrigger.Run";
        }

        Type[] ITriggerAttribute.GetRequiredParameterTypes()
        {
            return new[] { typeof(CloudBlockBlob) };
        }
    }
}
