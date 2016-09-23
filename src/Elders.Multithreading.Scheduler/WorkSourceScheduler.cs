using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elders.Multithreading.Scheduler.Logging;

namespace Elders.Multithreading.Scheduler
{
    internal class WorkSourceScheduler
    {
        private const int OneHour = 3600000;

        private static readonly ILog log = LogProvider.GetLogger(typeof(WorkSourceScheduler));

        private volatile bool shouldWork = true;

        private int sleepTime;

        private ConcurrentQueue<IWork> unmanagedPendingWork;

        private List<IWork> workForSchedule;

        private Thread workManager;

        private ManualResetEvent workManagerWaitHandle = new ManualResetEvent(false);

        private Action<IWork> workReadyForProcess;

        public WorkSourceScheduler()
        {
            workForSchedule = new List<IWork>();
            unmanagedPendingWork = new ConcurrentQueue<IWork>();
        }

        public void OnWorkReadyForProcess(Action<IWork> onWorkReadyForProcess)
        {
            workReadyForProcess = onWorkReadyForProcess;
        }

        public void ScheduleWork(IWork work)
        {
            unmanagedPendingWork.Enqueue(work);
            log.Debug(() => $"WorkManager received a new work for scheduling. [{work}]");
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
            workManager.Name = "Work Manager";
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
                    sleepTime = OneHour;
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
                            if (log.IsDebugEnabled())
                                log.DebugFormat("WorkManager has prepared a work for processing by the crawlers. [{0}]", orderedPendingWork[i]);
                        }
                        else
                        {
                            string error = "Probably there is a thread mess. We are trying to remove an item from 'workForSchedule' collection but it is missing. This should never happen because this collection must be served by only one thread.";
                            log.Error(error);
                            throw new InvalidOperationException(error);
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
                                : OneHour;
                        }
                        else
                        {
                            sleepTime = 0;
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                log.FatalException("Catastrophic error in WorkManager. PAFA!", ex);
                throw;
            }
        }

        private void PrepareToRescheduleAllUnmanagedPendingWork()
        {
            IWork job;
            while (unmanagedPendingWork.TryDequeue(out job))
            {
                workForSchedule.Add(job);
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
            if (workManagerWaitHandle != null)
                workManagerWaitHandle.Set();
        }
    }
}
