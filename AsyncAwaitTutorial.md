# Async / Await: From Zero to Hero

I had absolutely no idea what `async` / `await` was and learning it was **hard** as: 

1. There's **27 minutes** worth of text to read in the first two introductory articles by MSDN [here](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/) and [here](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/task-asynchronous-programming-model), with many more articles referenced in them.
1. It wasn't clearly stated in the documentation what `async` / `await` solves.
1. There's no one-stop guide concerning this topic.

I'm documenting my learnings to address the above pain points that I encountered, and the content is ordered as such:

1. Must Know
1. Should Know
1. Good to Know

This post assumes prior understanding of [threads](https://stackoverflow.com/a/1096986/8828382).

## Must Know

**Main Point**: `async` / `await` solves the problem of threads being blocked (waiting idly) while waiting for its task to complete. 

### Introduction

It's a weekend afternoon, and you decide to:

1. Use a waffle machine to make a waffle.
1. Reply a text message from your mum. 

In this hypothetical scenario,

1. Making a waffle is an asynchronous operation - you would leave the waffle mixture in the machine and let the machine make the waffle. This frees you to perform other tasks while waiting for the waffle to be completed.
1. Replying mum is a synchronous operation.

These operations, if implemented in a fully synchronous manner in C# code, may look like this:

```cs
static void Main()
{
    Waffle waffle = MakeWaffle();
    ReplyMum();
}

static Waffle MakeWaffle() 
{
    var task = Task.Delay(2000); // start the waffle machine. Simulates time taken to make the waffle
    task.Wait(); // synchronously wait for it...
    return new Waffle(); // waffle is done!
}

static void ReplyMum() => Thread.Sleep(1000); // simulates time taken by you to reply mum
```

### Problem

The thread calling `task.Wait()` is blocked till `task` completes.

This leads to inefficiency as you would `ReplyMum()` **after** `MakeWaffle()` has completed execution, rather than replying **while** `MakeWaffle()` is executing. Therefore, these tasks take roughly 2000ms + 1000ms = 3s to complete rather than the expected 2s.

### Solution

Let's update `MakeWaffle()` to run asynchronously:

```diff
-static Waffle MakeWaffle()
+static async Task<Waffle> MakeWaffleAsync() // (2) & (3)
 {
     var task = Task.Delay(2000);
-    task.Wait();
+    await task; // (1)
     return new Waffle();
 }
```

1. Replacing `Wait()` with `await`. `await` can be conceived of as the asynchronous version of `Wait()`. You would `ReplyMum()` immediately after starting the waffle machine, rather than waiting idly for the waffle machine to complete making the waffle.
1. Addition of `async` modifier in the method signature. This modifier is required to use the `await` keyword in the method; the compiler will complain otherwise.
1. Modifying the return type to `Task<Waffle>`. A `Task` object basically "represents the ongoing work". More on that below.

Let's update the caller method accordingly:

```diff
-static void Main()
+static async Task MainAsync()
 {
-    Waffle waffle = MakeWaffle();
+    Task<Waffle> waffleTask = MakeWaffleAsync();
     ReplyMum();
+    Waffle waffle = await waffleTask;
 }
```

The resulting code looks like this:

```cs
static async Task MainAsync()
{
	Task<Waffle> waffleTask = MakeWaffleAsync(); // (3)
	ReplyMum(); // (4)
	Waffle waffle = await waffleTask; // (5) & (7)
	// do something with waffle. Maybe eat it?
}

static async Task<Waffle> MakeWaffleAsync()
{
    var task = Task.Delay(2000); // (1)
    await task; // (2)
    return new Waffle(); // (6)
}

static void ReplyMum() => Thread.Sleep(1000);
```

Let's analyse the code:

1. Start the waffle machine.
1. Wait asynchronously for the waffle machine to complete making the waffle. Since the waffle is not yet done, control is returned to the caller.
1. `waffleTask` now references the incomplete task.
1. Start replying mum.
1. Wait asynchronously (remaining ~1s) for the waffle machine to complete making the waffle. In our scenario, since the main method has no caller, there's no caller to return control to and no further work for the thread to process.
1. Waffle machine is done making the waffle.
1. Assign the result of `waffleTask` to `waffle`.

Key clarifications:

1. Don't `await` a task too early; `await` it only at the point when you need its result. This allows the thread to execute the subsequent code till the `await` statement. This is illustrated in the above code sample:

	a. Notice the control flow at step 2. After executing `await task`, control is returned to `MainAsync()`; code after the `await` statement (step 6) is not executed until `task` completes.

	a. Similarly, if `await waffleTask` was executed before `ReplyMum()` (i.e. immediately after step 3), `ReplyMum()` won't execute until `waffleTask` completes.

1. Suppose `ReplyMum()` takes longer than 2000ms to complete, then `await waffleTask` will return a value **immediately** since `waffleTask` has already completed.

And we're done! You can run my [program](https://github.com/Zhiyuan-Amos/AsyncAwaitDemo) to verify that the synchronous code takes 3s to execute, while the asynchronous code only takes 2s.

### Additional Notes

1. [Sahan](https://dev.to/sahan/demystifying-async-await-in-c-4hkb) puts it well that "tasks are not an abstraction over threads"; `async` != `multithreading`. The illustration above is an example of a single-threaded (i.e. tasks are completed by one person), asynchronous work. [Stephen Cleary](https://blog.stephencleary.com/2013/11/there-is-no-thread.html) explained how this works under the hood.

1. Suffix `Async` ["for methods that return awaitable types"](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap?redirectedfrom=MSDN#naming-parameters-and-return-types). For example, I've renamed `MakeWaffle()` to `MakeWaffleAsync()`.

## Should Know

### Introduction

Suppose you want to do something more complex instead:

1. Use a waffle machine to make a waffle.
1. Use a coffee maker to make a cup of coffee.
1. Download a camera app from Play Store.
1. After steps 1 & 3 are completed, snap a photo of the waffle.
1. After steps 2 & 3 are completed, snap a photo of the coffee.

If we only use the syntax we've learned above, the code looks like this:

```cs
static async Task MainAsync()
{
	Task<Waffle> waffleTask = MakeWaffleAsync();
	Task<Coffee> coffeeTask = MakeCoffeeAsync();
	Task<App> downloadCameraAppTask = DownloadCameraAppAsync();

	var waffle = await waffleTask;
	var coffee = await coffeeTask;
	var app = await downloadCameraAppTask;

	app.Snap(waffle);
	app.Snap(coffee);
}
```

### Problem

Suppose the timing taken for each task to complete is random. In the event `waffleTask` and `downloadCameraAppTask` completes first, you would want to `app.Snap(waffle)` **while** waiting for `coffeeTask` to complete. 

However, you will not do so as you are still `await`-ing the completion of `coffeeTask`; `app.Snap(waffle)` comes after the awaiting of `coffeeTask`. That's inefficient.

### Solution

Let's use task continuation and task composition to resolve the above problem:

```cs
static async Task MainAsync()
{
	Task<Waffle> waffleTask = MakeWaffleAsync();
	Task<Coffee> coffeeTask = MakeCoffeeAsync();
	Task<App> downloadCameraAppTask = DownloadCameraAppAsync();

	Task snapWaffleTask = Task.WhenAll(waffleTask, downloadCameraAppTask) // (1)
		.ContinueWith(_ => downloadCameraAppTask.Result.Snap(waffleTask.Result)); // (2)
	Task snapCoffeeTask = Task.WhenAll(coffeeTask, downloadCameraAppTask)
		.ContinueWith(_ => downloadCameraAppTask.Result.Snap(coffeeTask.Result));

	await Task.WhenAll(snapWaffleTask, snapCoffeeTask);
}
```

1. `WhenAll` creates a task that completes when both `waffleTask` and `downloadCameraAppTask` completes.
1. `ContinueWith` creates a task that executes asynchronously after the above task completes.

Now, you would continue with snapping a photo of the waffle after `waffleTask` and `downloadCameraAppTask` completes; `coffeeTask` is no longer a factor in determining when `downloadCameraAppTask.Result.Snap(waffleTask.Result)` is executed.

### Additional Notes:

1. `Result` ["blocks the calling thread until the asynchronous operation is complete"](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.result?view=netframework-4.8#remarks). However, it doesn't cause performance degradation in our scenario as we have `await`-ed for the tasks to complete. Therefore, `waffleTask.Result`, `coffeeTask.Result` and `downloadCameraAppTask.Result` will return a value immediately.

1. Use [`WhenAny`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenany?view=netframework-4.8) if you want the task to complete when any of the supplied tasks have completed.

## Good to Know

1. An asynchronous method can return `void` instead of `Task`, but it is [not advisable to do so](https://stackoverflow.com/questions/12144077/async-await-when-to-return-a-task-vs-void).

1. `await Task.WhenAll(snapWaffleTask, snapCoffeeTask)` can be replaced with `await snapWaffleTask; await snapCoffeeTask;`. However, there are [benefits](https://stackoverflow.com/questions/18310996/why-should-i-prefer-single-await-task-whenall-over-multiple-awaits) of not doing so.

1. The following method

	```
	static Task<Waffle> MakeWaffleAsync() => 
		return Task.Delay(2000).ContinueWith(_ => new Waffle());
	```

	Can also be written as an asynchronous method:

	```
	static async Task<Waffle> MakeWaffleAsync() => 
		return await Task.Delay(2000).ContinueWith(_ => new Waffle());
	```

	Both options have their [pros & cons](https://stackoverflow.com/questions/19098143/what-is-the-purpose-of-return-await-in-c) depending on the scenario.

1. The performance of .NET and UI applications can be improved by using [`ConfigureAwait(false)`](https://stackoverflow.com/questions/13489065/best-practice-to-call-configureawait-for-all-server-side-code).

1. Tangential to our topic: Don't create fake asynchronous methods by using `Task.Run` [incorrectly](https://blog.stephencleary.com/2013/10/taskrun-etiquette-and-proper-usage.html).

## Conclusion

Hopefully, you found this guide helpful in your understanding of `async` / `await`. There are other topics that I didn't cover such as task cancellation, as I haven't had the need to use them. Knowing the above should be sufficient to kick-start your journey into asynchronous programming.

If you liked it, please give it a ❤️ or a 🦄, and do let me know your thoughts in the section below :)
