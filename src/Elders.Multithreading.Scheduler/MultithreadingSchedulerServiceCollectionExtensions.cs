using Microsoft.Extensions.DependencyInjection;

namespace Elders.Multithreading.Scheduler;

public static class MultithreadingSchedulerServiceCollectionExtensions
{
    public static IServiceCollection AddMultithreadingScheduler(this IServiceCollection services)
    {
        services.AddSingleton<WorkPoolFactory>();

        return services;
    }
}
