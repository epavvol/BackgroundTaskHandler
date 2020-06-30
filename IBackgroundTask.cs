using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Background task interface.
    /// </summary>
    public interface IBackgroundTask
    {
        /// <summary>
        /// Result message
        /// </summary>
        string? ResultMessage { get; }

        /// <summary>
        /// Warning message
        /// </summary>
        string? WarningMessage { get; }

        /// <summary>
        /// Task handling method
        /// </summary>
        /// <param name="configuration">Optional <c>Dictionary[String, String]</c> configuration object.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><c>true</c> if execution was successful, otherwise <c>false</c>.</returns>
        Task<bool> ExecuteAsync(Dictionary<string, string>? configuration, CancellationToken cancellationToken);
    }
}