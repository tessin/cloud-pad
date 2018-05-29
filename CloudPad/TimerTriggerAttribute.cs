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

        public TimerTriggerAttribute(string cronExpression)
        {
            CronExpression = cronExpression;
        }
    }
}
