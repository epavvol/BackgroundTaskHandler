using System.Collections.Generic;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Interface for BackgroundTaskEntry to filter properties
    /// Helps with registering new tasks
    /// </summary>
    public interface IBackgroundTaskEntry
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long TaskId { get; }

        /// <summary>
        /// List of child tasks
        /// </summary>
        public ICollection<BackgroundTaskEntry>? ChildTasks { get; set; }
    }
}