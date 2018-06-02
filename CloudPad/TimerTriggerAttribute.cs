using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TimerTriggerAttribute : Attribute
    {
        public string CronExpression { get; }

        /// <summary>
        /// Specific to CloudPad, can be used to change the default behavior when testing locally. This does not have any bearing on the Azure function.json setting `runOnStartup`.
        /// </summary>
        public bool? RunAtStartup { get; set; }

        public TimerTriggerAttribute(string cronExpression)
        {
            CronExpression = cronExpression;
        }
    }
}
