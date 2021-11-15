using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Elders.Multithreading.Scheduler;

public class WorkPool
{
    private readonly List<WorkProcessor> crawlers;

    private int jobsInSource = 0;

    private readonly string name;

    private readonly int numberOfWorkProcessors;
    private readonly ILogger<WorkPool> logger;
    private WorkSource workSource;

    /// <summary>
    /// Creates and instance of the CrawlerWorkPool
    /// </summary>
    /// <param name="name">Pool name</param>
    /// <param name="numberOfCrawlers">Specifies the number of crawlers to serve the pool</param>
    internal WorkPool(string name, int numberOfWorkProcessors, ILogger<WorkPool> logger)
    {
        this.name = name;
        this.workSource = new WorkSource(logger);
        this.numberOfWorkProcessors = numberOfWorkProcessors;
        this.logger = logger;
        this.crawlers = new List<WorkProcessor>();
    }

    /// <summary>
    /// Adds work to the pool's <see cref="CrawlerWorkSource"/> 
    /// </summary>
    /// <param name="job"></param>
    public void AddWork(IWork job)
    {
        workSource.RegisterWork(job);
        jobsInSource++;
    }

    /// <summary>
    /// Starts the pool's crawlers
    /// </summary>
    public void StartCrawlers()
    {
        for (int i = 1; i <= numberOfWorkProcessors && i <= jobsInSource; i++)
        {
            string threadName = $"Pool: '{name}' \t Crawler: '{i}'";
            WorkProcessor crw = new WorkProcessor(logger);
            crw.StartCrawling(threadName, workSource);
            crawlers.Add(crw);
        }
    }

    /// <summary>
    /// Stops the pool's crawlers
    /// </summary>
    public void Stop()
    {
        crawlers.ForEach(crawler => crawler.Stop());
        crawlers.Clear();

        if (workSource is not null)
        {
            workSource.DisposeSource();
            workSource = null;
        }
    }
}
