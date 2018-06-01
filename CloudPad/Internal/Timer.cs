using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;

namespace CloudPad.Internal
{
    class Timer
    {
        private readonly object _context;
        private readonly MethodInfo _method;

        public CrontabSchedule Schedule { get; }

        public Timer(object context, MethodInfo method, CrontabSchedule schedule)
        {
            _context = context;
            _method = method;

            this.Schedule = schedule;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Debug.Append($"timer {_method.Name} created, running...");

            var returnValue = _method.Invoke(_context, null); // parameterless
            if (returnValue is Task)
            {
                await (Task)returnValue;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var tick = DateTime.UtcNow;
                var next = Schedule.GetNextOccurrence(tick);
                var wait = next - tick;

                Log.Debug.Append($"timer {_method.Name} next run scheduled at {next:o}");

                if (TimeSpan.Zero < wait)
                {
                    await Task.Delay(wait, cancellationToken);
                }

                Log.Debug.Append($"timer {_method.Name} elapsed, running...");

                var returnValue2 = _method.Invoke(_context, null); // parameterless   
                if (returnValue2 is Task)
                {
                    await (Task)returnValue2;
                }
            }
        }
    }
}
