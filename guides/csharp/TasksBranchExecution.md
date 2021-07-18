##### The Users Challenge
I have to `await` two (maybe more) `Tasks` that call 3rd party services and those calls are really slow. They don't depend on each
other... is there anything I can do to speed things up from my side?

##### Task-based Branch Execution
Eventually most C# developers learn about `async` and the corresponding keyword `await`. It is a rite of passage!
    
The use case 99% of the time is that there is an `async Function` returning a `Task` (maybe `Task<T>`) that you know you have to `await`. I
admit that this is not as clean as `golang` and `goroutines` but C# `Tasks` and `Task<T>` are still incredibly powerful concepts that when
used right, can really help with performance not just responsiveness.

Let's take the mental handcuffs off of how you typically see the standard `Task` usage for a second. If we are allowed to modify
the following bit of code we can improve the overall performance without knowing much - if anything - of what's going on beneath the hood.
    
You can probably follow this example if you understand that we normally use `async` and `await` to execute an expensive call without
blocking the calling thread. In this following example, we have the two slow `async` operations (`Tasks`) being properly `awaited`
by the developer. There is nothing wrong with the internal code (assumption) but they are slow and that we have to wait
on each one to finish before continuing our executions.

They are written now as non-blocking, but they will execute in order _**sequentially**_.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tasks
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Start!");

            // Executing an async Task and awaiting.
            await ProcessMessageAsync("test0"); // this finishes first, we wait here till that happens.
            await ProcessMessageAsync("test1"); // this finishes second, we wait here till that happens.
        } // we make it here when everything else finishes.

        public static async Task ProcessMessageAsync(string input)
        {
            await Task.Yield(); // prevents this from executing synchronously
            await Console.Out.WriteLineAsync($"Processing Message: {input}");
        }
    }
}
```

What makes everything proceed in an orderly fashion is the use of `await`. The developer achieved the good design of non-blocking
operations but there is no concurrency due to the same mechanism - the `await`. Which leads me to: there is no rule you have to `await` code
that is `async`.
    
_**WARNING: That also doesn't mean go batshit crazy not using `await` in your code.**_

What I really should clarify is, you don't always have to `await` **here** like where we did in the above example. `Await` ensures that the
execution finishes and that it is also not lost to the GC ether. In other words, we want to use `await` and not using it generally causes
unintended nasty side-effects (like code not even executing)!
        
Here though, let us imagine if we did not use `await`? What would happen assuming `ProcessMessageAsync` was a schedulable operation?
As soon as the code finished invoking the first `ProcessMessageAsync`, it would begin to invoke the next line of code without stopping
or `awaiting`.

```text
If using await ensures the integrity of our executions, where do we put await?  
I still need it right? 
```
Yes!

By altering the above example, we can invoke (start) both `Tasks` (that are independent of each other) concurrently,
store a reference to these operations into a local variable (called `task1` etc.), then use those references as inputs to
`Task.WhenAll()` allowing us to `await` them all.

##### TL;DR
1. Branch out the execution of our two (or more) methods.  
2. Then "rejoin" to this `execution context` with `Task.WhenAll()`.  

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tasks
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Start!");

            // Tasks holding the operation and future (result). 
            var task0 = ProcessMessageAsync("test0"); // begin executing (branch #0)
            var task1 = ProcessMessageAsync("test1"); // begin executing (branch #1)
            
            // Non-blocking await - we wait here with out blocking the thread till all the input Tasks have Completed.
            await Task.WhenAll(task1, task2);
        }

        public static async Task ProcessMessageAsync(string input)
        {
            await Task.Yield(); // prevents this from executing synchronously
            await Console.Out.WriteLineAsync($"Processing Message: {input}");
        }
    }
}
```

If `task0` usually takes 30 seconds and `task1` is usually 30 seconds, our starting example code would take a total time of 60 seconds.
    
By re-arranging when we call the `await` (till after they have both started executing) we now have a `task0` taking 30 seconds and
`task1` taking 30 seconds concurrently. Our method total execution time is now only 30 seconds (or which ever of the tasks was
longest) as they happened concurrently.

#### This doesn't always occur...
This is the general use case, but this isn't a guarantee of execution. This code operates more like an instruction/suggestion. The execution
is not fully guaranteed. For a lot of use cases this is exactly how it works, but it is based on how busy the `TaskScheduler/ThreadPool` is
or if it determines this is executed immediately.

That is a rather complex concept and worth a whole separate and detailed article. As long as the `Task` is properly `async`, able to be
scheduled, then these can execute concurrently. Some examples would be a call out to a web.api, a save to a database etc.

_**Note: To demonstrate execution immediately, you can remove the `await Task.Yield();` from ProcessMessageAsync. You will then see it execute
in order.**_

#### What happens when you have more than two tasks?
Well you can use the same concept of `await Task.WhenAll()`. Here is what that could look like.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tasks
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Start!");

            // All my messages
            var myStrings = new List<string>
            {
                "test0",
                "test1",
                "test2",
                "test3",
                "test4",
                "test5",
                "test6",
                "test7",
                "test8",
                "test9"
            };

            // All my messages, assigned to a Task assigned inside an array.
            // Execution of each task begins when created but there is no blocking here.
            var tasks = new Task[myStrings.Count];
            for (int i = 0; i < myStrings.Count; i++)
            {
                tasks[i] = ProcessMessageAsync(myStrings[i]);
            }

            // Non-blocking await till all tasks are finished.
            await Task.WhenAll(tasks);
        }

        public static async Task ProcessMessageAsync(string input)
        {
            await Task.Yield(); // prevents this from executing synchronously
            await Console.Out.WriteLineAsync($"Processing Message: {input}");
        }
    }
}
```

The same possible decrease on execution time is possible, leading to significantly reduced total execution time. This is really only true
though when are able to create indepenent execution "branches" and the code is not dependent on the previous `task` having to finish.

#### Conclusion
Sometimes it just takes being mindful of how you use `async` and `await` to greatly increase execution performance. Other times, it requires
heavy refactoring. Production scenarios are rarely ever as easy as the above scenario demonstrates.

Some weaknesses to this strategy are:
1. Not all code will execute concurrently/parallely.
   i. This is due to advanced scheduling algorithm for a variety of reasons like a `hot for loop`.
      a. There are mechanisms that force scheduling to occur such as `Task.Run` or `Task.Yield` and will make the execution occur on a background
      thread. 
2. If your workload is not even, you will be prone to burst traffic.
   i. This means that there can overhall hiccups in performance, or bottlenecks on unrelated portions of the application.
      a. The execution resources are shared application wide unless you have created an independent `TaskScheduler`.
3. Exceptions can stop the execution of remaining tasks.
   i. This may be desireable though.
    
Example #3 would be best handled with a different approach because the context of the situation changes having a lot more work to `await`.