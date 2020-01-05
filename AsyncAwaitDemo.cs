using System;
using System.Threading;
using System.Threading.Tasks;

class AsyncAwaitDemo
{
    static async Task Main()
    {
        var timeBeforeExecutionSync = DateTime.Now;
        WeekendToDo();
        var timeAfterExecutionSync = DateTime.Now;
        Console.WriteLine("Sync Duration: {0}", timeAfterExecutionSync.Subtract(timeBeforeExecutionSync).TotalSeconds.ToString("#"));

        var timeBeforeExecutionAsync = DateTime.Now;
        await WeekendToDoAsync();
        var timeAfterExecutionAsync = DateTime.Now;
        Console.WriteLine("Async Duration: {0}", timeAfterExecutionAsync.Subtract(timeBeforeExecutionAsync).TotalSeconds.ToString("#"));
    }

    // <-- synchronous code -->

    static void WeekendToDo()
    {
        Waffle waffle = MakeWaffle();
        ReplyMum();
    }

    static Waffle MakeWaffle()
    {
        var task = Task.Delay(2000);
        task.Wait();
        return new Waffle();
    }

	static void ReplyMum() => Thread.Sleep(1000);

    // <-- asynchronous code -->

	static async Task WeekendToDoAsync()
	{
        Task<Waffle> waffleTask = MakeWaffleAsync();
		ReplyMum();
		Waffle waffle = await waffleTask;
	}

    static async Task<Waffle> MakeWaffleAsync()
    {
        var task = Task.Delay(2000);
        await task;
        return new Waffle();
    }
}

class Waffle {}
