using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CloudPad.Internal
{
    enum BindingType
    {
        None,
        HttpTrigger,
        TimerTrigger
    }

    class Binding
    {
        private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public BindingType Type { get; }
        public MethodInfo Method { get; }

        // http trigger
        public string Route { get; private set; }
        // todo: verbs and allow anonymous

        // timer trigger
        public string CronExpression { get; private set; }

        public Binding(BindingType type, MethodInfo method)
        {
            Type = type;
            Method = method;
        }

        public static Binding GetBinding(Type contextType, string methodName)
        {
            var m = contextType.GetMethod(methodName, _bindingFlags);
            if (m == null)
            {
                return null;
            }
            return GetBinding(m);
        }

        public static Binding GetBinding(MethodInfo m)
        {
            var httpTrigger = m.GetCustomAttribute<RouteAttribute>(); // todo: replace with HttpTrigger
            if (httpTrigger != null)
            {
                var binding = new Binding(BindingType.HttpTrigger, m)
                {
                    Route = httpTrigger.Template 
                };

                // todo: additional HTTP binding properties

                return binding;
            }

            var timerTrigger = m.GetCustomAttribute<TimerTriggerAttribute>();
            if (timerTrigger != null)
            {
                return new Binding(BindingType.TimerTrigger, m) { CronExpression = timerTrigger.CronExpression };
            }

            return null;
        }
    }
}
