using System;
using System.Collections.Generic;

namespace CloudPad
{
    public interface ITimerInfo
    {
        /// <summary>
        /// Gets the next occurrence of the schedule based on the specified base time.
        /// </summary>
        DateTime GetNextOccurrence(DateTime? now = null);

        /// <summary>
        /// Returns a collection of the next 'count' occurrences of the schedule, starting from now.
        /// </summary>
        IEnumerable<DateTime> GetNextOccurrences(int count, DateTime? now = null);

        /// <summary>
        /// The last recorded schedule occurrence
        /// </summary>
        DateTime Last { get; }

        /// <summary>
        /// The expected next schedule occurrence
        /// </summary>
        DateTime Next { get; }


        /// <summary>
        /// The last time this record was updated. This is used to re-calculate Next with the current Schedule after a host restart.
        /// </summary>
        DateTime Updated { get; }


        /// <summary>
        /// Gets or sets a value indicating whether this timer invocation is due to a missed schedule occurrence.
        /// </summary>
        bool IsPastDue { get; }

        /// <summary>
        /// Formats the next 'count' occurrences of the schedule into an easily loggable string.
        /// </summary>
        string FormatNextOccurrences(int count, DateTime? now = null);
    }
}
