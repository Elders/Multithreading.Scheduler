using System;
using System.Threading;
using Elders.Multithreading.Scheduler.Logging;

namespace Elders.Multithreading.Scheduler
{
    /// <summary>
    /// This class represents a wraper of a dedicated thread which does countinous work over a <see cref="WorkSource"/>
    /// </summary>
    internal class WorkProcessor
    {
        private static readonly ILog log = LogProvider.GetLogger(typeof(WorkProcessor));

        private string name;

        private volatile bool shouldStop = false;

        private IWork work;

        private Thread thread;

        /// <summary>
        /// Creates an instace of a crawler.
        /// </summary>
        /// <param name="name">Defines the thread name of the crawler instance</param>
        public WorkProcessor(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Starts dedicated crawler's thread which countinously takes and executes work.
        /// </summary>
        /// <param name="workSource">Crawler worksource</param>
        public void StartCrawling(WorkSource workSource)
        {
            if (thread == null)
            {
                thread = new Thread(new ThreadStart(() =>
                {
                    while (!shouldStop)
                    {
                        try
                        {
                            log.Debug("Getting available work...");
                            work = workSource.GetAvailableWork();
                            if (work != null)
                            {
                                log.DebugFormat("Executing work [{0}]", work);
                                work.Start();
                                log.DebugFormat("Work finished successfully. [{0}]", work);
                                workSource.ReturnFinishedWork(work);
                                log.DebugFormat("Work returned to the source. [{0}]", work);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.ErrorException("Exception occured while executing a work. You should take care for all exceptions while you implement 'ICrawlerJob.Start()' method.", ex);
                        }
                    }
                    log.Info("Crowler was stopped.");
                }));
                thread.Name = name;
                thread.Start();
            }
            else
            {
                log.FatalFormat("Crawler '{0}' is already running on another source.", name);
                throw new InvalidOperationException(String.Format("Crawler '{0}' is already running on another source.", name));
            }
        }

        /// <summary>
        /// Tells to the crawler's thread to finish it's last work and exit.
        /// </summary>
        public void Stop()
        {
            try
            {
                log.DebugFormat("Stopping crawler '{0}'...", name);
                thread = null;
                shouldStop = true;
                if (work != null)
                    work.Stop();
            }
            catch (Exception ex)
            {
                log.FatalFormat("Exception occured while executing a work. You should take care for all exceptions while you implement 'ICrawlerJob.Stop()' method.", ex);
                throw;
            }
        }

    }
}
