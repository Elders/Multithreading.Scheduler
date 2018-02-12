Multithreading.Scheduler 
========================
[![Build status](https://ci.appveyor.com/api/projects/status/g83gf2h2mqdna2o8)](https://ci.appveyor.com/project/mynkow/multithreading-scheduler-929)


The library gives you a control over the lifecycle of threds. You can create a dedicated threads for a particular work. This means that the library is suitable for applications which need to do a background processing. It is a tradeoff. You gain performance boost because the context switching is removed, like the.NET thread pool does, but this allocates resources which could not be reused.

Usage 
=====


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
