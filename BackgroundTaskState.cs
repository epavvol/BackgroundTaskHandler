namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// BackgroundTaskState specification
    /// </summary>
    public enum BackgroundTaskState
    {
        /// <summary>
        /// Task is pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Task is currently in progress
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Task has been completed successfully
        /// </summary>
        CompletedSuccessfully = 2,

        /// <summary>
        /// Task has been completed successfully and have some warning message
        /// </summary>
        CompletedWithWarning = 3,

        /// <summary>
        /// Task has been failed
        /// Handler returns false as the result
        /// </summary>
        Failed = 4,

        /// <summary>
        /// During task execution the exception was thrown
        /// </summary>
        ExceptionWasThrown = 5,

        /// <summary>
        /// Task has been cancelled before it was processed
        /// </summary>
        Cancelled = 6,

        /// <summary>
        /// During task execution the <c>TimeoutException</c> was thrown
        /// </summary>
        TimedOut = 7
    }
}