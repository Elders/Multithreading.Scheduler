Multithreading.Scheduler 
========================
[![Build status](https://ci.appveyor.com/api/projects/status/g83gf2h2mqdna2o8)](https://ci.appveyor.com/project/mynkow/multithreading-scheduler-929)

Usage 
========================

The idea is to create a dedicated threads for a particular work. The WorkPool class does not use the standard .NET thread pool.

```C#
class Program
{
    static void Main(string[] args)
    {
        AtomicInteger state = new AtomicInteger();
        string poolName = "This name is assigned to the current Thread.Name";
        int numberOfThreadsAvailableForThePool = 5;

        WorkPool pool = new WorkPool(poolName, numberOfThreadsAvailableForThePool);
        for (int i = 0; i < numberOfThreadsAvailableForThePool; i++)
        {
            pool.AddWork(new IncrementByOne(state));
        }
        pool.StartCrawlers();

        Console.WriteLine("Press enter to exit...");
        Console.ReadLine();

        pool.Stop();
    }
}

public class IncrementByOne : IWork
{
    private readonly AtomicInteger state;

    public IncrementByOne(AtomicInteger state)
    {
        this.state = state;
    }

    public DateTime ScheduledStart { get; set; }

    public void Start()
    {
        state.Increment();
    }

    public void Stop()
    {
        //  Do cleanup here.
    }
}

public class AtomicInteger
{
    private int currentValue;

    public void Increment()
    {
        Interlocked.Increment(ref currentValue);
    }
}
```