using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// The service executes tasks in background.
    /// Based on entity framework core DbSet of BackgroundTaskEntry objects.
    /// </summary>
    public class BackgroundTaskHandlerService : IHostedService, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Dependency injection Service Provider
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Logger instance for BackgroundTaskHandlerService
        /// </summary>
        protected ILogger<BackgroundTaskHandlerService> Logger { get; }

        /// <summary>
        /// Options monitor callback token disposable.
        /// Used to stop monitoring of options object.
        /// </summary>
        private readonly IDisposable? _optionReloadToken;

        /// <summary>
        /// BackgroundTaskHandlerOptions active options.
        /// </summary>
        protected BackgroundTaskHandlerOptions Options { get; private set; }

        /// <summary>
        /// BackgroundTaskHandlerService execution count.
        /// </summary>
        protected virtual ulong ExecutionCount { get; set; }

        /// <summary>
        /// Count of background tasks processed.
        /// </summary>
        protected virtual ulong ProcessedCount { get; set; }

        /// <summary>
        /// Count of background tasks processed successfully.
        /// </summary>
        protected virtual ulong SuccessCount { get; set; }

        /// <summary>
        /// Timer that starting task checks.
        /// </summary>
        protected virtual Timer? Timer { get; set; }

        /// <summary>
        /// <c>true</c> if currently executing tasks, otherwise <c>false</c>.
        /// </summary>
        protected virtual bool CurrentlyRunning { get; set; }

        /// <summary>
        /// Dictionary keeping currently running tasks.
        /// </summary>
        protected virtual Dictionary<Task, BackgroundTaskEntry> BackgroundTasks { get; set; }
            = new Dictionary<Task, BackgroundTaskEntry>();

        /// <summary>
        /// Background task handler service constructor
        /// </summary>
        /// <param name="logger">ILogger instance to log service actions</param>
        /// <param name="serviceServiceProvider">Dependency injection service provider</param>
        /// <param name="optionsMonitor"><c>IOptionMonitor</c> instance to get current configuration.</param>
        public BackgroundTaskHandlerService(ILogger<BackgroundTaskHandlerService> logger,
            IServiceProvider serviceServiceProvider, IOptionsMonitor<BackgroundTaskHandlerOptions> optionsMonitor)
        {
            Logger = logger;
            ServiceProvider = serviceServiceProvider;
            _optionReloadToken = optionsMonitor
                .OnChange(ReloadOptions);
            Options = optionsMonitor.CurrentValue;
        }

        /// <summary>
        /// Method called once configuration was updated.
        /// </summary>
        /// <param name="options">Options object instance</param>
        protected virtual void ReloadOptions(BackgroundTaskHandlerOptions options)
        {
            Logger.LogInformation(
                $"{nameof(BackgroundTaskHandlerOptions)} is reloading its configuration, " +
                $"interval changed to {options.Interval.ToString()} seconds.");
            Options = options;
            Timer?.Change(TimeSpan.Zero,
                TimeSpan.FromSeconds(Options.Interval));
        }

        /// <summary>
        /// Service start point
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation(
                $"{nameof(BackgroundTaskHandlerService)} starting up " +
                $"with interval of {Options.Interval.ToString()} seconds.");
            Timer = new Timer(
                async _ => await ExecuteAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(Options.Interval));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Main execution method called on schedule
        /// </summary>
        protected virtual async Task ExecuteAsync(bool wait = false)
        {
            Logger.LogInformation("Execution started!");
            // Check if previous execution is still in progress
            if (CurrentlyRunning && !wait)
                return;

            if (wait && CurrentlyRunning)
                await Task.Run(async () =>
                {
                    while (CurrentlyRunning)
                        await Task.Delay(500);
                });

            CurrentlyRunning = true;

            // Increase execution count by 1
            ExecutionCount++;

            // Check if there is at lease one completed task or there is a room for new tasks,
            // otherwise end execution
            if (!BackgroundTasks.Keys.Any(x => x.IsCompleted) &&
                BackgroundTasks.Count >= Options.ParallelTaskCountLimit)
                return;

            // Creating dependency injection scope for current execution
            using var serviceScope = ServiceProvider.CreateScope();

            // Getting database connection (used by service only)
            using var dbContext = serviceScope
                .ServiceProvider
                .GetRequiredService<IBackgroundTaskHandlerDbContext>();

            // Gathering finished tasks
            var completedTasks = BackgroundTasks
                .Where(x =>
                    x.Value.TaskState != BackgroundTaskState.InProgress)
                .ToArray();

            // Finalizing tasks, saving changes to database and removing from memory
            foreach (var (task, entry) in completedTasks)
            {
                Console.WriteLine(
                    $"Completed task {task.Id} with entry [{entry.TaskId}] {entry.TaskName} of type {entry.TaskTypeName}");
                dbContext.GetBackgroundTaskDbSet().Update(entry);
                SuccessCount++;
                BackgroundTasks.Remove(task);
            }

            // Saving changes to database
            await dbContext.SaveChangesAsync();

            // Gather new tasks that could be processed
            var newTasks = await dbContext
                .GetBackgroundTaskDbSet()
                .AsNoTracking()
                .Include(entry => entry.ChildTasks)
                .Where(entry =>
                    entry.TaskState == BackgroundTaskState.Pending &&
                    (entry.NotBefore == null || entry.NotBefore <= DateTime.Now) &&
                    !entry.ChildTasks.Any(child =>
                        child.TaskState <= BackgroundTaskState.InProgress))
                .OrderBy(entry => entry.TaskId)
                .Take(Options.ParallelTaskCountLimit - BackgroundTasks.Count)
                .ToArrayAsync();

            // Register new tasks
            foreach (var entry in newTasks)
                RegisterNewTask(entry);

            // Saving changes to database
            await dbContext.SaveChangesAsync();

            // Setting execution is not active
            CurrentlyRunning = false;
        }

        private void RegisterNewTask(BackgroundTaskEntry entry) =>
            BackgroundTasks.TryAdd(
                Task<Task<BackgroundTaskResult>>.Factory.StartNew(async () =>
                {
                    using var serviceScope = ServiceProvider.CreateScope();

                    // Trying to create instance of current task type instance to handle task
                    // First checking if dependency injection contains required service,
                    // Then trying to create instance with parameterless constructor.
                    if (!(serviceScope
                        .ServiceProvider
                        .GetService(entry.TaskType) is IBackgroundTask backgroundTaskHandler))
                    {
                        try
                        {
                            backgroundTaskHandler = (IBackgroundTask) Activator.CreateInstance(entry.TaskType);
                        }
                        catch (MissingMethodException)
                        {
                            throw new InvalidOperationException(
                                $"Type '{entry.TaskTypeName}' does not have parameterless constructor " +
                                "and was not registered in dependency injection service container.");
                        }
                    }

                    byte tryCount = 0;
                    var timeOut = 1000 * (entry.TimeOut ?? 0);
                    while (true)
                    {
                        try
                        {
                            CancellationTokenSource? cts = null;
                            if (timeOut > 0)
                            {
                                cts = new CancellationTokenSource();
                                cts.CancelAfter(timeOut);
                            }

                            var executionResult = await backgroundTaskHandler.ExecuteAsync(entry.Configuration,
                                cts?.Token ?? CancellationToken.None);
                            cts?.Dispose();
                            return new BackgroundTaskResult(
                                executionResult,
                                backgroundTaskHandler.ResultMessage,
                                backgroundTaskHandler.WarningMessage);
                        }
                        catch (TimeoutException)
                        {
                            throw;
                        }
                        catch
                        {
                            if (tryCount >= entry.RetryCount)
                                throw;
                            tryCount++;
                        }
                    }
                }).ContinueWith(async taskOfStartNew =>
                    await (await taskOfStartNew)
                        .ContinueWith(mainTask => CompleteBackgroundTask(entry, mainTask))),
                entry);

        private static void CompleteBackgroundTask(BackgroundTaskEntry entry, Task<BackgroundTaskResult> task)
        {
            task.Wait();
            if (task.IsFaulted)
            {
                entry.TaskState = task.Exception.InnerExceptions.Any(e => e is TimeoutException)
                    ? BackgroundTaskState.TimedOut
                    : BackgroundTaskState.ExceptionWasThrown;

                entry.ExceptionMessage = task.Exception.ToString();
            }
            else if (task.IsCompletedSuccessfully && task.Result.Success)
                entry.TaskState = string.IsNullOrWhiteSpace(task.Result.WarningMessage)
                    ? BackgroundTaskState.CompletedSuccessfully
                    : BackgroundTaskState.CompletedWithWarning;
            else
                entry.TaskState = BackgroundTaskState.Failed;

            entry.ResultMessage = task.Result.ResultMessage;
            entry.WarningMessage = task.Result.WarningMessage;
        }

        /// <summary>
        /// Service finishing point
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            Timer?.Change(Timeout.Infinite, 0);
            await ExecuteAsync(true);
            Logger.LogInformation($"{nameof(BackgroundTaskHandlerService)} is stopping! " +
                                  $"In total {ProcessedCount} tasks were processed ({SuccessCount} successfully) in {ExecutionCount} executions.");
        }

        /// <summary>
        /// Object dispose method
        /// </summary>
        public virtual void Dispose()
        {
            _optionReloadToken?.Dispose();
            Timer?.Dispose();
        }

        /// <summary>
        /// Object dispose asynchronous method
        /// </summary>
        public virtual async ValueTask DisposeAsync()
        {
            _optionReloadToken?.Dispose();
            if (Timer != null)
                await Timer.DisposeAsync();
        }

        /// <summary>
        /// Class represents results of task processed
        /// used for updating task registry entry
        /// </summary>
        protected class BackgroundTaskResult
        {
            /// <summary>
            /// BackgroundTaskResult constructor
            /// </summary>
            /// <param name="success">Task processing result</param>
            /// <param name="resultMessage">Task processing result message</param>
            /// <param name="warningMessage">Task processing warning</param>
            public BackgroundTaskResult(bool success, string? resultMessage = null, string? warningMessage = null)
            {
                Success = success;
                ResultMessage = resultMessage;
                WarningMessage = warningMessage;
            }

            /// <summary>
            /// Task processing result. <c>true</c> if successful, otherwise <c>false</c>.
            /// </summary>
            public bool Success { get; }

            /// <summary>
            /// Task processing result message
            /// </summary>
            public string? ResultMessage { get; }

            /// <summary>
            /// Task processing warning
            /// </summary>
            public string? WarningMessage { get; }
        }
    }
}