using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Elders.Multithreading.Scheduler;

internal class WorkSourceScheduler
{
    const string ThreadMessError = "Probably there is a thread mess. We are trying to remove an item from 'workForSchedule' collection but it is missing. This should never happen because this collection must be served by only one thread.";

    private const int OneSecond = 1000;
    private readonly ILogger logger;
    private volatile bool shouldWork = true;

    private int sleepTime;

    private ConcurrentQueue<IWork> unmanagedPendingWork;

    private List<IWork> workForSchedule;

    private Thread workManager;

    private ManualResetEvent workManagerWaitHandle = new ManualResetEvent(false);

    private Action<IWork> workReadyForProcess;

    public WorkSourceScheduler(ILogger logger)
    {
        workForSchedule = new List<IWork>();
        unmanagedPendingWork = new ConcurrentQueue<IWork>();
        this.logger = logger;
    }

    public void OnWorkReadyForProcess(Action<IWork> onWorkReadyForProcess)
    {
        workReadyForProcess = onWorkReadyForProcess;
    }

    public void ScheduleWork(IWork work)
    {
        unmanagedPendingWork.Enqueue(work);
        WakeUpTheManager();
    }

    public void StartManage()
    {
        workManager = new Thread(new ThreadStart(() =>
        {
            while (shouldWork)
            {
                ManageWork();
                Sleep();
            }
        }));
        workManager.Name = "WorkManager";
        workManager.Start();
    }

    public void Stop()
    {
        shouldWork = false;
        WakeUpTheManager();

        workReadyForProcess = null;
        workManagerWaitHandle = null;
        unmanagedPendingWork = new ConcurrentQueue<IWork>();
        workForSchedule = new List<IWork>();
    }

    private bool IsThereAnyWorkForRescheduling()
    {
        return workForSchedule.Count == 0;
    }

    private void ManageWork()
    {
        try
        {
            PrepareToRescheduleAllUnmanagedPendingWork();

            if (IsThereAnyWorkForRescheduling())
            {
                sleepTime = OneSecond;
                return;
            }

            var orderedPendingWork = workForSchedule.OrderBy(x => x.ScheduledStart).ToList();

            for (int i = 0; i < orderedPendingWork.Count; i++)
            {
                if ((orderedPendingWork[i].IsReadyToStart()))
                {
                    if (workForSchedule.Remove(orderedPendingWork[i]))
                    {
                        workReadyForProcess(orderedPendingWork[i]);
                        if (logger.IsEnabled(LogLevel.Debug))
                            logger.LogDebug($"WorkManager has prepared a work for processing by the crawlers. [{orderedPendingWork[i].Name}]");
                    }
                    else
                    {
                        logger.LogError(ThreadMessError);
                        throw new InvalidOperationException(ThreadMessError);
                    }
                }
                else
                {
                    //  The WorkManager goes to sleep if there is NO work for scheduling at the moment. The sleep interval is the timespan between current time and the time when next work
                    //  should be scheduled.
                    var waitTime = (orderedPendingWork[i].ScheduledStart.Subtract(DateTime.UtcNow));
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        sleepTime = int.MaxValue > (long)waitTime.TotalMilliseconds
                            ? Convert.ToInt32(waitTime.TotalMilliseconds)
                            : OneSecond;
                    }
                    else
                    {
                        sleepTime = 0;
                    }
                    return;
                }
            }
        }
        catch (Exception ex) when (logger.ErrorWithScope(ex, "Catastrophic error in WorkManager. PAFA!"))
        {
            throw;
        }
    }

    private void PrepareToRescheduleAllUnmanagedPendingWork()
    {
        while (unmanagedPendingWork.TryDequeue(out IWork work))
        {
            workForSchedule.Add(work);
        }
    }

    private void Sleep()
    {
        if (sleepTime > 0)
        {
            workManagerWaitHandle?.Reset();
            workManagerWaitHandle?.WaitOne(sleepTime);
        }
    }

    private void WakeUpTheManager()
    {
        if (workManagerWaitHandle is not null)
            workManagerWaitHandle.Set();
    }
}
