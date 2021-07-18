### The Challenge - How do I do Parallel ForEach Async?
I would like to execute a function on a `List` whose input parameter is each element of my `List<T>`.  
e.g., Perform this function against each integer in my list of ints.  

Oh that's eas.... *LOUD SCREECHING BRAKE NOISES*.
        
##### Additional Ask  
I would like for it to not block (asynchronous) and fire in parallel/concurrently to decrease execution time,
but also not be prone to burst/spike traffic.

Oof. Unlike golang this isn't quite that easy *but it should be.* Let's make it happen :devilface:

##### IEnumerable Extension Method
Well this is both a straightforward request but also a little complicated as we don't quite have the built in tools for all of
these requirements. Luckily though, there are still a couple of options to get started with.

The following handy method I have written was ready to go. I have used it a few times is really good for simple use cases where
you just need to do `X()` against each `Y` element.
        
Is it it perfect? No, but it works really well for what it does and very easy to implement.

The idea behind this method is that you have a List of items and you want to fire the same Function/Action against each
element of the list. This scenario is extremely common in backend services. You have multiples of a `Thing` and this `Thing`
has to be saved to a `database` for example. The item could be an `Order` and the action could be `SaveToDatabaseAsync().` Another
example, could be charge payments from a queue, where you are given a list of payments to process and there is no bulk upload
solution. 
        
The example usages goes on and on.

How this works:  
1. Take an IEnumerable and dissect it into partitions.
    * The partition count is determined by your `maxDoP` value.
        * This will determine how many actions are firing max at any one time.
2. For each Partition, in parallel, get the current element.
3. Invoke the supplied function with that element as the input argument.
4. Repeat till all partitions' elements have been processed or until exception occurs.

```cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelForEachAsync
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Start!");

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

            // For each string in myStrings, parallelly and asynchronously, call ProcessMessageAsync against each element.
            // Limit the maximum calls to logical processor count of the environment.
            await myStrings.ParallelForEachAsync(ProcessMessageAsync, Environment.ProcessorCount);
        }

        public static async Task ProcessMessageAsync(string input)
        {
            await Console.Out.WriteLineAsync($"Processing Message: {input}");
        }

        public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> funcBody, int maxDoP = 4)
        {
            async Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        await Task.Yield(); // prevents a sync/hot thread hangup
                        await funcBody(partition.Current);
                    }
                }
            }

            return Task.WhenAll(
                Partitioner
                    .Create(source)
                    .GetPartitions(maxDoP)
                    .AsParallel()
                    .Select(p => AwaitPartition(p)));
        }
    }
}
```
We added `await Task.Yield()` to force asynchronous scheduled pattern. This is absolutely necessary for scenarios where the
`Task` is CPU heavy immediately and would prevent scheduling/concurrency. Instead of using Task.Run(() =>) for the entire thing
I used PLINQ (`AsParallel()`). In my mind was cleaner and matched the coding pattern while also performing nearly the same thing.
I used a `local function` instead of a `lambda` because they perform better and are lower on the allocations.

##### Features
* You want the performance to be adjustable (maxDoP).
    * This allows you to control the "burst" of resources used by setting a maximum cap to simultaneously process.
* You want the function to be generic so it is re-usable code.
* It is written as a convenience extension method that all IEnumerables could perform.
* If an exception occurs it occurs here and not in the background.
* Will handle high computation `Task` that block further scheduling/executions till it finishes (hot loop etc.).
* It is also looks incredibly clean/sexy if you ask me.

##### Disadvantage
* Borderline close to the ActionBlock/Dataflow use case.
* Exceptions interfere with unfinished executions (but that maybe desireable).
* Not the most efficient Parallel way of doing it but still plenty faster than synchronous and sequential calls.
    * Still one of the cleanest.
