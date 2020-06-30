using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// EF Database context interface required for background task handler service
    /// </summary>
    public interface IBackgroundTaskHandlerDbContext : IDisposable
    {
        /// <summary>
        /// Returns DBSet of BackgroundTaskEntry objects.
        /// </summary>
        /// <returns>DBSet of BackgroundTaskEntry objects</returns>
        DbSet<BackgroundTaskEntry> GetBackgroundTaskDbSet();

        /// <summary>
        /// Saves context changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of added entries</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}