using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Elders.Multithreading.Scheduler;

/// <summary>
/// This class represents a wraper of a dedicated thread which does countinous work over a <see cref="WorkSource"/>
/// </summary>
internal class WorkProcessor
{
    private readonly ILogger<WorkPool> logger;
    private volatile bool shouldStop = false;

    private IWork work;

    private Thread thread;

    /// <summary>
    /// Creates an instace of a crawler.
    /// </summary>
    /// <param name="name">Defines the thread name of the crawler instance</param>
    public WorkProcessor(ILogger<WorkPool> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Starts dedicated crawler's thread which countinously takes and executes work.
    /// </summary>
    /// <param name="workSource">Crawler worksource</param>
    public void StartCrawling(string name, WorkSource workSource)
    {
        if (thread is null)
        {
            thread = new Thread(new ThreadStart(() =>
            {
                using (logger.BeginScope(new Dictionary<string, object> { { "Crawler", name } })) // I know that name is a string, but please save urself 5 hours and do not change the value of the dictionary to string. ElasticSearch logging would not work due to mappings not being able to resolve the type!
                {
                    while (!shouldStop)
                    {
                        try
                        {
                            if (logger.IsEnabled(LogLevel.Debug))
                                logger.LogDebug("Getting available work...");

                            work = workSource.GetAvailableWork();

                            if (work is not null)
                            {
                                using (logger.BeginScope(new Dictionary<string, object> { { "CrawlerWork", work.Name } }))  // I know that name is a string, but please save urself 5 hours and do not change the value of the dictionary to string. ElasticSearch logging would not work due to mappings not being able to resolve the type!
                                {
                                    if (logger.IsEnabled(LogLevel.Debug))
                                        logger.LogDebug($"Executing work...");

                                    work.Start();

                                    if (logger.IsEnabled(LogLevel.Debug))
                                        logger.LogDebug($"Work finished successfully!");

                                    workSource.ReturnFinishedWork(work);

                                    if (logger.IsEnabled(LogLevel.Debug))
                                        logger.LogDebug($"Work returned to the source.");
                                }
                            }
                        }
                        catch (Exception ex) when (logger.ErrorWithScope(ex, "Exception occured while executing a work. You should take care for all exceptions while you implement 'ICrawlerJob.Start()' method.")) { }
                    }
                    logger.LogInformation("Crawler has been stopped.");
                }
            }));
            thread.Name = name;
            thread.Start();
        }
        else
        {
            string message = $"Crawler '{name}' is already running on another source.";
            logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Tells to the crawler's thread to finish it's last work and exit.
    /// </summary>
    public void Stop()
    {
        try
        {
            logger.LogDebug("Stopping crawler..");
            shouldStop = true;
            if (work is not null)
                work.Stop();
            thread = null;
        }
        catch (Exception ex)
        {
            logger.LogCritical("Exception occured while executing a work. You should take care for all exceptions while you implement 'ICrawlerJob.Stop()' method.", ex);
            throw;
        }
    }

}

public static class LogExtension
{
    public static bool ErrorWithScope(this ILogger logger, Exception ex, string message = null)
    {
        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(ex, message ?? ex.Message);

        return true;
    }
}
