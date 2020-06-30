using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Background task registration entry
    /// </summary>
    public class BackgroundTaskEntry : IBackgroundTaskEntry
    {
        /// <summary>
        /// Task ID
        /// </summary>
        [Key]
        public long TaskId { get; set; }

        /// <summary>
        /// State of the task
        /// </summary>
        public BackgroundTaskState TaskState { get; set; } = BackgroundTaskState.Pending;

        /// <summary>
        /// Delayed task, task will not be executed before that time
        /// </summary>
        public DateTime? NotBefore { get; set; }

        /// <summary>
        /// Amount of times handler should retry before fail
        /// </summary>
        public byte RetryCount { get; set; }

        /// <summary>
        /// Maximum execution time
        /// </summary>
        public short? TimeOut { get; set; }

        /// <summary>
        /// Type of the class to be used to handle this task
        /// </summary>
        [NotMapped]
        public Type TaskType { get; set; } = default!;

        /// <summary>
        /// Full name of the class type to be used to handle this task
        /// </summary>
        [MaxLength(2048)]
        public string TaskTypeName
        {
            get => TaskType == null ? default! : $"{TaskType.FullName}, {TaskType.Assembly.FullName}";
            set => TaskType = Type.GetType(value);
        }

        /// <summary>
        /// Id of parent task in tree
        /// </summary>
        public long? ParentTaskId { get; set; }

        /// <summary>
        /// Parent task in tree
        /// </summary>
        public BackgroundTaskEntry? ParentTask { get; set; }

        /// <summary>
        /// List of child tasks
        /// </summary>
        public ICollection<BackgroundTaskEntry>? ChildTasks { get; set; }

        /// <summary>
        /// User, that owns the task
        /// </summary>
        [MaxLength(256)]
        public string? Owner { get; set; }

        /// <summary>
        /// Name of the task
        /// </summary>
        [MaxLength(128)]
        public string TaskName { get; set; } = default!;

        /// <summary>
        /// Exception thrown during handling process
        /// </summary>
        [MaxLength(4000)]
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Dictionary of string elements that will be passed to execute method
        /// </summary>
        [NotMapped]
        public Dictionary<string, string>? Configuration { get; set; }

        /// <summary>
        /// Configuration dictionary in JSON format
        /// </summary>
        [MaxLength(4000)]
        public string? ConfigurationString
        {
            get => Configuration == null
                ? null
                : System.Text.Json.JsonSerializer.Serialize(Configuration);
            set => Configuration = value == null
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        }

        /// <summary>
        /// Result message
        /// </summary>
        [MaxLength(2048)]
        public string? ResultMessage { get; set; }

        /// <summary>
        /// Warning message
        /// </summary>
        [MaxLength(2048)]
        public string? WarningMessage { get; set; }

        /// <summary>
        /// Task cancellation
        /// </summary>
        /// <exception cref="InvalidOperationException">Will be thrown if task is not in Pending state</exception>
        public void Cancel()
        {
            if (TaskState != BackgroundTaskState.Pending && TaskState != BackgroundTaskState.Cancelled)
                throw new InvalidOperationException("Only pending tasks can be cancelled.");
            TaskState = BackgroundTaskState.Cancelled;
        }
    }
}