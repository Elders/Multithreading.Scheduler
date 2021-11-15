using Microsoft.Extensions.Logging;

namespace Elders.Multithreading.Scheduler;

public class WorkPoolFactory
{
    private readonly ILogger<WorkPool> logger;

    public WorkPoolFactory(ILogger<WorkPool> logger)
    {
        this.logger = logger;
    }

    public WorkPool Create(string name, int numberOfWorkProcessors)
    {
        return new WorkPool(name, numberOfWorkProcessors, logger);
    }
}
