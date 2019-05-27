using CloudPad.Internal;
using Newtonsoft.Json.Linq;
using System;

namespace CloudPad
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TimerTriggerAttribute : Attribute, ITriggerAttribute
    {
        /// <summary>
        /// Gets the schedule expression.
        /// </summary>
        public string ScheduleExpression { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the schedule should be monitored. Schedule monitoring persists schedule occurrences to aid in ensuring the schedule is maintained correctly even when roles restart. If not set explicitly, this will default to true for schedules that have a recurrence interval greater than 1 minute (i.e., for schedules that occur more than once per minute, persistence will be disabled).
        /// </summary>
        public bool UseMonitor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the function should be invoked immediately on startup. After the initial startup run, the function will be run on schedule thereafter.
        /// </summary>
        public bool RunOnStartup { get; set; }

        public TimerTriggerAttribute(string scheduleExpression)
        {
            this.ScheduleExpression = scheduleExpression;
        }

        object ITriggerAttribute.GetBindings()
        {
            var bindings = new JArray();

            var timerTrigger = new JObject();

            timerTrigger["type"] = "timerTrigger";
            timerTrigger["schedule"] = ScheduleExpression;
            timerTrigger["useMonitor"] = UseMonitor;
            timerTrigger["runOnStartup"] = RunOnStartup;
            timerTrigger["name"] = "timer";
            timerTrigger["direction"] = "in";

            bindings.Add(timerTrigger);

            return bindings;
        }

        string ITriggerAttribute.GetEntryPoint()
        {
            return "CloudPad.FunctionApp.TimerTrigger.Run";
        }

        Type[] ITriggerAttribute.GetRequiredParameterTypes()
        {
            return new Type[0];
        }
    }
}
