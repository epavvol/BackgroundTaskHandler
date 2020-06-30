using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Set of extension methods that help to create new tasks to be handled in background.
    /// </summary>
    public static class BackgroundTaskCreationExtensions
    {
        /// <summary>
        /// Creates new task for background processing and adds to database.
        /// </summary>
        /// <param name="dbSet">DBSet of task entries</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>DbSet[BackgroundTaskEntry]</c></returns>
        public static DbSet<BackgroundTaskEntry> CreateTask<T>(
            this DbSet<BackgroundTaskEntry> dbSet,
            string taskName,
            Action<IBackgroundTaskEntry> taskActions
        )
            where T : class, IBackgroundTask => CreateTask<T>(
            dbSet, taskName, null, null, null,
            null, null, taskActions);

        /// <summary>
        /// Creates new task for background processing and adds to database.
        /// </summary>
        /// <param name="dbSet">DBSet of task entries</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <param name="configuration">Dictionary object that will be passed to handler</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>DbSet[BackgroundTaskEntry]</c></returns>
        public static DbSet<BackgroundTaskEntry> CreateTask<T>(
            this DbSet<BackgroundTaskEntry> dbSet,
            string taskName,
            Dictionary<string, string> configuration,
            Action<IBackgroundTaskEntry>? taskActions = null
        )
            where T : class, IBackgroundTask => CreateTask<T>(
            dbSet, taskName, configuration, null, null,
            null, null, taskActions);

        /// <summary>
        /// Creates new task for background processing and adds to database.
        /// </summary>
        /// <param name="dbSet">DBSet of task entries</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <param name="configuration">Dictionary object that will be passed to handler</param>
        /// <param name="retryCount">How many times task will be executed in case of failure</param>
        /// <param name="notBefore">Delayed task, will be executed not earlier than specified time</param>
        /// <param name="timeOut">Timeout for task in seconds.</param>
        /// <param name="owner">Owner of the task</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>DbSet[BackgroundTaskEntry]</c></returns>
        public static DbSet<BackgroundTaskEntry> CreateTask<T>(
            this DbSet<BackgroundTaskEntry> dbSet,
            string taskName,
            Dictionary<string, string>? configuration = null,
            byte? retryCount = null,
            DateTime? notBefore = null,
            short? timeOut = null,
            string? owner = null,
            Action<IBackgroundTaskEntry>? taskActions = null
        )
            where T : class, IBackgroundTask
        {
            var entry = CreateTask<T>(taskName, configuration, retryCount, notBefore, timeOut, owner);
            taskActions?.Invoke(entry);
            dbSet.Add(entry);
            return dbSet;
        }

        /// <summary>
        /// Creates new task for background processing and adds to parent task
        /// </summary>
        /// <param name="parentTask">Task that will depend on this task</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>IBackgroundTaskEntry</c></returns>
        public static IBackgroundTaskEntry CreateSubTask<T>(
            this IBackgroundTaskEntry parentTask,
            string taskName,
            Action<IBackgroundTaskEntry> taskActions
        )
            where T : class, IBackgroundTask => CreateSubTask<T>(
            parentTask, taskName, null, null,
            null, null, null, taskActions);

        /// <summary>
        /// Creates new task for background processing and adds to parent task
        /// </summary>
        /// <param name="parentTask">Task that will depend on this task</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <param name="configuration">Dictionary object that will be passed to handler</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>IBackgroundTaskEntry</c></returns>
        public static IBackgroundTaskEntry CreateSubTask<T>(
            this IBackgroundTaskEntry parentTask,
            string taskName,
            Dictionary<string, string> configuration,
            Action<IBackgroundTaskEntry>? taskActions = null
        )
            where T : class, IBackgroundTask => CreateSubTask<T>(
            parentTask, taskName, configuration, null,
            null, null, null, taskActions);

        /// <summary>
        /// Creates new task for background processing and adds to parent task
        /// </summary>
        /// <param name="parentTask">Task that will depend on this task</param>
        /// <param name="taskName">Name of task that can be displayed to user</param>
        /// <param name="taskActions">Action that helps to configure task/add sub tasks</param>
        /// <param name="configuration">Dictionary object that will be passed to handler</param>
        /// <param name="retryCount">How many times task will be executed in case of failure</param>
        /// <param name="notBefore">Delayed task, will be executed not earlier than specified time</param>
        /// <param name="timeOut">Timeout for task in seconds.</param>
        /// <param name="owner">Owner of the task</param>
        /// <typeparam name="T">Task handler type</typeparam>
        /// <returns><c>IBackgroundTaskEntry</c></returns>
        public static IBackgroundTaskEntry CreateSubTask<T>(
            this IBackgroundTaskEntry parentTask,
            string taskName,
            Dictionary<string, string>? configuration = null,
            byte? retryCount = null,
            DateTime? notBefore = null,
            short? timeOut = null,
            string? owner = null,
            Action<IBackgroundTaskEntry>? taskActions = null
        )
            where T : class, IBackgroundTask
        {
            if (parentTask.TaskId > 0)
                throw new InvalidOperationException("Cannot assign sub-task to already registered task!");
            parentTask.ChildTasks ??= new List<BackgroundTaskEntry>();
            var entry = CreateTask<T>(taskName, configuration, retryCount, notBefore, timeOut, owner);
            taskActions?.Invoke(entry);
            parentTask.ChildTasks.Add(entry);
            return parentTask;
        }

        private static BackgroundTaskEntry CreateTask<T>(
            string taskName,
            Dictionary<string, string>? configuration = null,
            byte? retryCount = null,
            DateTime? notBefore = null,
            short? timeOut = null,
            string? owner = null
        )
            where T : class, IBackgroundTask => new BackgroundTaskEntry
        {
            TaskType = typeof(T),
            TaskName = taskName,
            Configuration = configuration,
            RetryCount = retryCount ?? 0,
            NotBefore = notBefore,
            TimeOut = timeOut,
            Owner = owner
        };
    }
}