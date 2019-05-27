using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;

namespace CloudPad
{
    class TimerInfoWrapper : ITimerInfo
    {
        private readonly TimerInfo _timer;
        
        public DateTime Last => _timer.ScheduleStatus.Last;
        public DateTime Next => _timer.ScheduleStatus.Next;
        public DateTime Updated => _timer.ScheduleStatus.LastUpdated;

        public bool IsPastDue => _timer.IsPastDue;

        public TimerInfoWrapper(TimerInfo timer)
        {
            _timer = timer;
        }

        public DateTime GetNextOccurrence(DateTime? now = null)
        {
            return _timer.Schedule.GetNextOccurrence(now ?? DateTime.Now);
        }

        public IEnumerable<DateTime> GetNextOccurrences(int count, DateTime? now = null)
        {
            return _timer.Schedule.GetNextOccurrences(count, now);
        }

        public string FormatNextOccurrences(int count, DateTime? now = null)
        {
            return _timer.FormatNextOccurrences(count, now);
        }
    }
}
