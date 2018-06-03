using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    class TimerFunction
    {
        public Function Function { get; }
        private readonly NCrontab.CrontabSchedule _schedule;

        public TimerFunction(Function function, NCrontab.CrontabSchedule schedule)
        {
            this.Function = function;
            this._schedule = schedule;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (Function.Binding.RunAtStartup ?? true)
            {
                await Function.InvokeAsync(cancellationToken: cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;

                var next = _schedule.GetNextOccurrence(utcNow);
                var wait = next - utcNow;

                await Task.Delay(wait);

                await Function.InvokeAsync(cancellationToken: cancellationToken);
            }
        }
    }

    class TimerServer : IDisposable
    {
        private readonly List<TimerFunction> _functions = new List<TimerFunction>();

        public void RegisterFunction(Function function)
        {
            var cronExpression = function.Binding.CronExpression;

            var schedule = NCrontab.CrontabSchedule.TryParse(cronExpression, new NCrontab.CrontabSchedule.ParseOptions { IncludingSeconds = true }); // Azure function runtime V1 does not support anything else
            if (schedule == null)
            {
                throw new ArgumentException($"cannot parse function '{function.Name}' CRON expression", nameof(function));
            }

            _functions.Add(new TimerFunction(function, schedule));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (var function in _functions)
            {
                tasks.Add(function.RunAsync(cancellationToken));
            }

            var task = await Task.WhenAny(tasks);
            await task; // unwrap
        }

        public Task InvokeAsync(string functionName, CancellationToken cancellationToken)
        {
            var function = _functions.First(f => f.Function.Name == functionName);
            return function.Function.InvokeAsync(cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            // no op
        }
    }
}
