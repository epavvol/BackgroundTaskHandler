using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VNetDev.BackgroundTaskHandler
{
    /// <summary>
    /// Set of extension methods to register background task handler service and
    /// handling classes to dependency injection service container.
    /// </summary>
    public static class BackgroundTaskHandlerServiceCollectionExtension
    {
        /// <summary>
        /// Register service to dependency injection container
        /// </summary>
        /// <param name="serviceCollection">Dependency injection container</param>
        /// <param name="optionsAction">Manual service configuration, this will overwrite settings in appsettings.json</param>
        /// <typeparam name="TDbContext">EF Database Context type</typeparam>
        /// <returns>Dependency injection container</returns>
        public static IServiceCollection AddBackgroundTaskHandler<TDbContext>(
            this IServiceCollection serviceCollection,
            Action<BackgroundTaskHandlerOptions>? optionsAction = null)
            where TDbContext : DbContext, IBackgroundTaskHandlerDbContext
        {
            // Adding configuration
            if (optionsAction == null)
            {
                // Adding section if optionsAction is not defined
                var configuration = serviceCollection
                    .BuildServiceProvider()
                    .GetService<IConfiguration>();

                if (configuration == null)
                {
                    // Adding default configuration if IConfiguration service is not available
                    serviceCollection.Configure<BackgroundTaskHandlerOptions>(options => { });
                }
                else
                {
                    // Binding options to configuration section provided by IConfiguration
                    serviceCollection.Configure<BackgroundTaskHandlerOptions>(
                        configuration.GetSection(
                            BackgroundTaskHandlerOptions.ConfigurationSection));
                }
            }
            else
            {
                // Adding configuration provided in optionsAction
                // Note: in this case configuration section will be ignored
                serviceCollection.Configure(optionsAction);
            }

            // Adding IBackgroundTaskHandlerDbContext service to resolve TDBContext
            serviceCollection.AddTransient<IBackgroundTaskHandlerDbContext>(x =>
                x.GetRequiredService<TDbContext>());

            // Adding BackgroundTaskHandlerService Service
            serviceCollection.AddHostedService<BackgroundTaskHandlerService>();

            return serviceCollection;
        }

        /// <summary>
        /// Resisters <c>IBackgroundTask</c> implementation for tasks handling
        /// </summary>
        /// <param name="serviceCollection">Dependency injection container</param>
        /// <typeparam name="T">Type to be registered</typeparam>
        /// <returns>Dependency injection container</returns>
        public static IServiceCollection AddBackgroundTaskHandlerType<T>(
            this IServiceCollection serviceCollection)
            where T : class, IBackgroundTask
        {
            serviceCollection.TryAddTransient<T>();
            return serviceCollection;
        }
    }
}