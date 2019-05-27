using CloudPad.Internal;
using Newtonsoft.Json.Linq;
using System;

namespace CloudPad {
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class QueueTriggerAttribute : Attribute, ITriggerAttribute {
    public string QueueName { get; set; }

    public QueueTriggerAttribute(string queueName) {
      this.QueueName = queueName;
    }

    object ITriggerAttribute.GetBindings() {
      var bindings = new JArray();

      var queueTrigger = new JObject();

      queueTrigger["type"] = "queueTrigger";
      queueTrigger["direction"] = "in";
      if (QueueName != null) {
        queueTrigger["queueName"] = QueueName;
      }
      queueTrigger["name"] = "msg";

      bindings.Add(queueTrigger);

      return bindings;
    }

    string ITriggerAttribute.GetEntryPoint() {
      return "CloudPad.FunctionApp.QueueTrigger.Run";
    }

    Type[] ITriggerAttribute.GetRequiredParameterTypes() {
      return new Type[0];
    }
  }
}
