using System;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Background task handler configuration object
    /// </summary>
    public class BackgroundTaskHandlerOptions
    {
        internal const string ConfigurationSection = "BackgroundTaskHandler";

        private byte _interval = 20;

        /// <summary>
        /// BackgroundTask handling interval
        /// </summary>
        public byte Interval
        {
            get => _interval;
            set => _interval = Math.Max(Math.Min(value, byte.MaxValue), (byte) 2);
        }

        /// <summary>
        /// Set the amount of tasks that can be handled in parallel
        /// Set 0 for unlimited
        /// </summary>
        public byte ParallelTaskCountLimit { get; set; } = 5;

        /// <summary>
        /// Fail task it its type was not found or cannot be instantiated
        /// If set to <c>true</c> then task will be set to failed state and exception will be logged.
        /// If set to <c>false</c> then task will remain pending and warning will be logged.
        /// </summary>
        public bool FailIfTypeNoTypeFound { get; set; } = false;
    }
}