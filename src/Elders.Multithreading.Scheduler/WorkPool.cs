using System;
using System.Collections.Generic;

namespace Elders.Multithreading.Scheduler
{
    public class WorkPool
    {
        private List<WorkProcessor> crawlers;

        private int jobsInSource = 0;

        private string poolName;

        private int numberOfWorkProcessors;

        private WorkSource workSource;

        /// <summary>
        /// Creates and instance of the CrawlerWorkPool
        /// </summary>
        /// <param name="poolName">Pool name</param>
        /// <param name="numberOfCrawlers">Specifies the number of crawlers to serve the pool</param>
        public WorkPool(string poolName, int numberOfWorkProcessors)
        {
            this.poolName = poolName;
            this.workSource = new WorkSource();
            this.numberOfWorkProcessors = numberOfWorkProcessors;
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
                WorkProcessor crw = new WorkProcessor(String.Format("Pool: '{0}' \t Crawler: '{1}'", poolName, i));
                crw.StartCrawling(workSource);
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

            if (workSource != null)
            {
                workSource.DisposeSource();
                workSource = null;
            }
        }
    }
}