using CloudPad.Internal;
using System;

namespace CloudPad {

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class BlobTriggerAttribute : Attribute, ITriggerAttribute {
    public string BlobPath { get; set; }

    public BlobTriggerAttribute(string blobPath) {
      this.BlobPath = blobPath;
    }

    object ITriggerAttribute.GetBindings() {
      throw new NotImplementedException();
    }

    string ITriggerAttribute.GetEntryPoint() {
      throw new NotImplementedException();
    }

    Type[] ITriggerAttribute.GetRequiredParameterTypes() {
      return new[] { typeof(Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob) };
    }
  }
}
