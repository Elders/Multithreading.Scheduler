﻿using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading;

namespace Elders.Multithreading.Scheduler;

internal class WorkSource
{
    private ConcurrentQueue<IWork> availableWork;

    private ConcurrentQueue<ManualResetEvent> freeCrawlers;

    private volatile bool IsSourceReleased = false;

    private WorkSourceScheduler workSourceScheduler;

    public WorkSource(ILogger<WorkPool> logger)
    {
        availableWork = new ConcurrentQueue<IWork>();
        freeCrawlers = new ConcurrentQueue<ManualResetEvent>();
        workSourceScheduler = new WorkSourceScheduler(logger);
        workSourceScheduler.OnWorkReadyForProcess(work =>
        {
            availableWork.Enqueue(work);
            NotifyFreeCrawler();
        });
        workSourceScheduler.StartManage();
    }

    /// <summary>
    /// Returns available <see cref="ICrawlerWork"/> or blocks the thread unitl work is available.
    /// </summary>
    /// <returns><see cref="ICrawlerWork"/> or null if the source is released.</returns>
    public IWork GetAvailableWork()
    {
        IWork work;
        while (!IsSourceReleased)
        {
            if (availableWork.TryDequeue(out work))
                return work;
            else
            {
                var handle = new ManualResetEvent(false);
                freeCrawlers.Enqueue(handle);
                handle.WaitOne(1000);
            }
        }

        return null;
    }

    /// <summary>
    /// Registers/Adds work to the <see cref="CrawlerWorkSource"/>.
    /// </summary>
    /// <param name="work"><see cref="ICrawlerWork"/></param>
    public void RegisterWork(IWork work)
    {
        if (work.IsReadyToStart())
        {
            availableWork.Enqueue(work);
        }
        else
        {
            workSourceScheduler.ScheduleWork(work);
        }
    }

    /// <summary>
    /// Releases all the work from the current <see cref="CrawlerWorkSource"/>, making it unavailable.
    /// </summary>
    public void DisposeSource()
    {
        IsSourceReleased = true;

        if (workSourceScheduler != null)
        {
            workSourceScheduler.Stop();
            workSourceScheduler = null;
        }

        while (freeCrawlers.TryDequeue(out ManualResetEvent handle))
        {
            handle.Set();
        }

        availableWork = new ConcurrentQueue<IWork>();
    }

    /// <summary>
    /// Returns <see cref="ICrawlerWork"/> to the current <see cref="CrawlerWorkSource"/> for rescheduling.
    /// </summary>
    /// <param name="work"></param>
    public void ReturnFinishedWork(IWork work)
    {
        if (!IsSourceReleased)
        {
            if (work.IsReadyToStart())
            {
                availableWork.Enqueue(work);
                NotifyFreeCrawler();
            }
            else
            {
                workSourceScheduler.ScheduleWork(work);
            }
        }
    }

    private void NotifyFreeCrawler()
    {
        if (freeCrawlers.IsEmpty == false)
        {
            if (freeCrawlers.TryDequeue(out ManualResetEvent handle))
                handle.Set();
        }
    }
}
