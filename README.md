# BackgroundTaskHandler
Simple Background Task Handler with database back-end

> This service's target is to execute background tasks in ASPNET applications registered in database by using entity framework and save the execution result.

#### Nuget package

https://www.nuget.org/packages/VNetDev.BackgroundTaskHandler/

### BackgroundTaskHandlerService registration

In order to register BackgroundTaskHandlerService we need to add it to the dependency injection container as an IHostedService instance by using following extension method:

```CSharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddBackgroundTaskHandler<AppDbContext>();
}
``` 

### BackgroundTask handler types

To create own handler for particular purpose it is required to implement IBackgroundTask interface.
If it has parameterless constructor it can be used as is, but if there are some dependencies that need to be resolved from service provider you need to register it to service collection by using following extension method:

```CSharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddBackgroundTaskHandlerType<SomeTask>();
}
```

All these methods have fluent interface and can be added to one another.

```CSharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddBackgroundTaskHandler<AppDbContext>()
        .AddBackgroundTaskHandlerType<FirstTask>()
        .AddBackgroundTaskHandlerType<SecondTask>()
        .AddBackgroundTaskHandlerType<ThirdTask>();
}
```

### Entity Framework Core database context requirements

EFCore database context used for BackgroundTaskHandler must implement ```IBackgroundTaskHandlerDbContext``` interface as show bellow.

```CSharp
public void ConfigureServices(IServiceCollection services)
{
    public class AppDbContext : DbContext, IBackgroundTaskHandlerDbContext
    {
        public AppDbContext() : base()
        {
        }

        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<BackgroundTaskEntry> SysBackgroundTasks { get; set; }

        public DbSet<BackgroundTaskEntry> GetBackgroundTaskDbSet() => SysBackgroundTasks;
    }
}
```

### New background task registration

In order to create new background task (or task tree) the ```CreateTask``` extension method must be used on ```DbSet<BackgroundTaskEntry>```.

```CSharp
public async Task<IActionResult> MyAspNetAction()
{
    // Simple task registration
    _dbContext.SysBackgroundTasks
        .CreateTask<SimpleTask>("MyTaskName");
    
    // Task tree registration
    _dbContext.SysBackgroundTasks
        .CreateTask<ImportantTask>(
            taskName:      "TaskDisplayName",
            configuration: new Dictionary<string, string>
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"}
            },
            retryCount:    3,
            notBefore:     DateTime.Now.AddMinutes(10),
            timeOut:       5,
            owner:         "UserName",
            taskActions:   entry => entry
                .CreateSubTask<SubTask>("FirstSubTaskDisplayName")
                .CreateSubTask<SubTask>("SecondSubTaskDisplayName"));
    
    await _dbContext.SaveChangesAsync();
    
    return Ok();
}
```
