using HouseofCat.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.Utilities.File;

public static class FileEnumerators
{
    #region Standard Enumeration

    public static IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern,
        SearchOption searchOpt)
    {
        try
        {
            var dirFiles = Enumerable.Empty<string>();
            if (searchOpt == SearchOption.AllDirectories)
            {
                dirFiles = Directory
                    .EnumerateDirectories(path)
                    .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
            }
            return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
        }
        catch (UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
    }

    #endregion

    #region Standard Enumeration w/ Parallel

    public static IEnumerable<string> EnumerateFilesInParallel(
        string path,
        string searchPattern,
        SearchOption searchOpt)
    {
        try
        {
            var procCount = Environment.ProcessorCount;
            var dirFiles = Enumerable.Empty<string>();
            if (searchOpt == SearchOption.AllDirectories)
            {
                dirFiles = Directory.EnumerateDirectories(path)
                                    .AsParallel()
                                    .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
            }
            return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern).AsParallel().WithDegreeOfParallelism(procCount));
        }
        catch (UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
    }

    public static IEnumerable<string> EnumerateFilesInParallelMaxDegree(
        string path,
        string searchPattern,
        SearchOption searchOpt)
    {
        try
        {
            var procCount = Environment.ProcessorCount;
            var dirFiles = Enumerable.Empty<string>();
            if (searchOpt == SearchOption.AllDirectories)
            {
                dirFiles = Directory.EnumerateDirectories(path)
                                    .AsParallel()
                                    .WithDegreeOfParallelism(procCount)
                                    .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
            }
            return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern).AsParallel().WithDegreeOfParallelism(procCount));
        }
        catch (UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
    }

    public static IEnumerable<string> EnumerateFilesInParallelMaxDegreeForced(
        string path,
        string searchPattern,
        SearchOption searchOpt)
    {
        try
        {
            var procCount = Environment.ProcessorCount;
            var dirFiles = Enumerable.Empty<string>();
            if (searchOpt == SearchOption.AllDirectories)
            {
                dirFiles = Directory.EnumerateDirectories(path)
                                    .AsParallel()
                                    .WithDegreeOfParallelism(procCount)
                                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                    .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
            }
            return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern).AsParallel().WithDegreeOfParallelism(procCount));
        }
        catch (UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
    }

    #endregion

    #region Parallel & Concurrent TreeTraverse

    public static async Task<IEnumerable<string>> GetFilesInQueueParallelAsync(
        string path,
        string searchPattern)
    {
        var t = await Task.Run(() =>
        {
            var bag = new ConcurrentBag<string>();

            QueueParallelTreeTraverse(path, searchPattern, (f) => { bag.Add(f); });

            return bag.ToList();
        }).ConfigureAwait(false);

        return t;
    }

    public static async Task<IEnumerable<string>> GetFilesInConcurrentParallelAsync(
        string path,
        string searchPattern)
    {
        var t = await Task.Run(() =>
        {
            var bag = new ConcurrentBag<string>();

            ConcurrentParallelTreeTraverse(path, searchPattern, (f) => { bag.Add(f); });

            return bag.ToList();
        }).ConfigureAwait(false);

        return t;
    }

    public static async Task<IEnumerable<string>> GetFilesInStackParallelAsync(
        string path,
        string searchPattern)
    {
        var t = await Task.Run(() =>
        {
            var bag = new ConcurrentBag<string>();

            StackParallelTreeTraverse(path, searchPattern, (f) => { bag.Add(f); });

            return bag.ToList();
        }).ConfigureAwait(false);

        return t;
    }

    public static void QueueParallelTreeTraverse(
        string root,
        string searchPattern,
        Action<string> action)
    {
        int fileCount = 0;
        int procCount = Environment.ProcessorCount;
        Queue<string> dirs = new Queue<string>();

        dirs.Enqueue(root);
        while (dirs.Count > 0)
        {
            var currentDir = dirs.Dequeue();
            var subDirs = Array.Empty<string>();
            var files = Array.Empty<string>();

            try { subDirs = Directory.GetDirectories(currentDir); }
            catch (SecurityException) { continue; }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }

            try { files = Directory.GetFiles(currentDir, searchPattern); }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            catch (IOException) { continue; }

            try
            {
                // Don't Run Small File Counts in Parallel
                // Overhead will beat the performance benefits.
                if (files.Length < procCount)
                {
                    foreach (var file in files)
                    {
                        action(file);
                        fileCount++;
                    }
                }
                else
                {
                    Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
                    {
                        action(file);
                        return ++localCount;
                    },
                    (c) => { Interlocked.Add(ref fileCount, c); });
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException) { return true; }

                    return false;
                });
            }

            foreach (var str in subDirs)
            { dirs.Enqueue(str); }
        }
    }

    public static void ConcurrentParallelTreeTraverse(
        string root,
        string searchPattern,
        Action<string> action)
    {
        int fileCount = 0;
        var procCount = Environment.ProcessorCount;
        var dirs = new ConcurrentQueue<string>();

        dirs.Enqueue(root);
        while (!dirs.IsEmpty)
        {
            string currentDir = string.Empty;
            if (dirs.TryDequeue(out currentDir))
            {
                var subDirs = Array.Empty<string>();
                var files = Array.Empty<string>();

                try { subDirs = Directory.GetDirectories(currentDir); }
                catch (SecurityException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                catch (DirectoryNotFoundException) { continue; }

                try { files = Directory.GetFiles(currentDir, searchPattern); }
                catch (UnauthorizedAccessException) { continue; }
                catch (DirectoryNotFoundException) { continue; }
                catch (IOException) { continue; }

                try
                {
                    // Don't Run Small File Counts in Parallel
                    // Overhead will beat the performance benefits.
                    if (files.Length < procCount)
                    {
                        foreach (var file in files)
                        {
                            action(file);
                            fileCount++;
                        }
                    }
                    else
                    {
                        Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
                        {
                            action(file);
                            return ++localCount;
                        },
                        (c) => { Interlocked.Add(ref fileCount, c); });
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is UnauthorizedAccessException) { return true; }

                        return false;
                    });
                }

                foreach (var str in subDirs)
                { dirs.Enqueue(str); }
            }
        }
    }

    public static void StackParallelTreeTraverse(
        string root,
        string searchPattern,
        Action<string> action)
    {
        int fileCount = 0;
        int procCount = Environment.ProcessorCount;
        Stack<string> dirs = new Stack<string>();

        dirs.Push(root);
        while (dirs.Count > 0)
        {
            var currentDir = dirs.Pop();
            var subDirs = Array.Empty<string>();
            var files = Array.Empty<string>();

            try { subDirs = Directory.GetDirectories(currentDir); }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }

            try { files = Directory.GetFiles(currentDir, searchPattern); }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            catch (IOException) { continue; }

            try
            {
                // Don't Run Small File Counts in Parallel
                // Overhead will beat the performance benefits.
                if (files.Length < procCount)
                {
                    foreach (var file in files)
                    {
                        action(file);
                        fileCount++;
                    }
                }
                else
                {
                    Parallel.ForEach(files, () => 0,
                    (file, loopState, localCount) =>
                    {
                        action(file);
                        return ++localCount;
                    },
                    (c) =>
                    { Interlocked.Add(ref fileCount, c); });
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException) { return true; }

                    return false;
                });
            }

            foreach (var str in subDirs)
            { dirs.Push(str); }
        }
    }

    #endregion 

    #region TreeTraverse Pseudo-Recursion & Async File Enumeration

    public static async Task<IEnumerable<string>> GetFilesAsync(
        string path,
        string searchPattern)
    {
        return await GetFileNamesAsync(path, searchPattern).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<string>> EnumerateFilesWithTasksTreesConcurrentAndParallelAsync(string path, string searchPattern)
    {
        return await ConcurrentTaskTreeTraverseInParallelAsync(path, searchPattern).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<string>> TreeTraverseAsync(string root,
        string searchPattern)
    {
        var taskBag = new ConcurrentBag<Task>();
        var fileNameBags = new ConcurrentBag<ConcurrentBag<string>>();

        var directories = new ConcurrentQueue<string>();
        directories.Enqueue(root);

        while (!directories.IsEmpty)
        {
            string currentDir = string.Empty;
            if (directories.TryDequeue(out currentDir))
            {
                await GetDirectoriesAsync(currentDir, directories).ConfigureAwait(false);
                taskBag.Add(GetFileNamesAsync(currentDir, searchPattern, fileNameBags));
            }
        }

        await Task.WhenAll(taskBag).ConfigureAwait(false);

        return fileNameBags.AsParallel().SelectMany(f => f);
    }

    public static async Task<IEnumerable<string>> ConcurrentTaskTreeTraverseInParallelAsync(
        string root,
        string searchPattern)
    {
        var sw = Stopwatch.StartNew();
        var taskBag = new ConcurrentBag<Task>();
        var fileNameBags = new ConcurrentBag<ConcurrentBag<string>>();

        var directoryQueue = new ConcurrentQueue<ConcurrentQueue<string>>();
        var directory = new ConcurrentQueue<string>();

        directory.Enqueue(root);
        directoryQueue.Enqueue(directory);

        while (!directoryQueue.IsEmpty)
        {
            if (directoryQueue.TryDequeue(out ConcurrentQueue<string> dirs))
            {
                await dirs.DequeueExisting().ParallelForEachAsync(
                    async dir =>
                    {
                        await GetDirectoryQueuesAsync(dir, directoryQueue).ConfigureAwait(false);
                        taskBag.Add(GetFileNamesAsync(dir, searchPattern, fileNameBags));
                    }).ConfigureAwait(false);
            }
        }

        sw.Stop();

        return fileNameBags.AsParallel().SelectMany(f => f);
    }

    private static async Task GetDirectoriesAsync(
        string directory,
        ConcurrentQueue<string> directories)
    {
        await Task.Run(() =>
        {
            var subDirs = Array.Empty<string>();

            try
            { subDirs = Directory.GetDirectories(directory); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }

            foreach (var dir in subDirs) { directories.Enqueue(dir); };
        }).ConfigureAwait(false);
    }

    private static async Task GetDirectoryQueuesAsync(
        string directory,
        ConcurrentQueue<ConcurrentQueue<string>> directoryQueue)
    {
        await Task.Run(() =>
        {
            var subDirs = Array.Empty<string>();

            try
            { subDirs = Directory.GetDirectories(directory); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }

            var dirs = new ConcurrentQueue<string>();
            foreach (var dir in subDirs) { dirs.Enqueue(dir); };

            directoryQueue.Enqueue(dirs);
        }).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<string>> GetFileNamesAsync(
        string directory,
        string searchPattern)
    {
        var t = await Task.Run(() =>
        {
            var files = Array.Empty<string>();
            var fileNames = new ConcurrentBag<string>();

            try
            { files = Directory.GetFiles(directory, searchPattern); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }
            catch (IOException) { /* SWALLOW */ }

            try
            {
                foreach (var file in files) { fileNames.Add(file); }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException) { return true; }

                    return false;
                });
            }

            return fileNames;
        }).ConfigureAwait(false);

        return t;
    }

    private static async Task GetFileNamesAsync(
        string directory,
        string searchPattern,
        ConcurrentBag<ConcurrentBag<string>> fileNameBags)
    {
        await Task.Run(() =>
        {
            var files = Array.Empty<string>();
            var fileNames = new ConcurrentBag<string>();

            try
            { files = Directory.GetFiles(directory, searchPattern); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }
            catch (IOException) { /* SWALLOW */ }

            try
            {
                foreach (var file in files) { fileNames.Add(file); };
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException) { return true; }

                    return false;
                });
            }

            fileNameBags.Add(fileNames);
        }).ConfigureAwait(false);
    }

    private static async Task GetDirectoriesInParallelAsync(
        string directory,
        ConcurrentQueue<string> directories)
    {
        await Task.Run(() =>
        {
            var subDirs = Array.Empty<string>();

            try
            { subDirs = Directory.GetDirectories(directory); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }

            Parallel.ForEach(subDirs, dir => { directories.Enqueue(dir); });
        }).ConfigureAwait(false);
    }

    private static async Task GetFileNamesInParallelAsync(
        string directory,
        string searchPattern,
        ConcurrentBag<string> fileNames)
    {
        await Task.Run(() =>
        {
            var files = Array.Empty<string>();

            try
            { files = Directory.GetFiles(directory, searchPattern); }
            catch (UnauthorizedAccessException) { /* SWALLOW */ }
            catch (DirectoryNotFoundException) { /* SWALLOW */ }
            catch (IOException) { /* SWALLOW */ }

            try
            {
                Parallel.ForEach(files, file => { fileNames.Add(file); });
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException) { return true; }

                    return false;
                });
            }

            return fileNames.ToList();
        }).ConfigureAwait(false);
    }

    #endregion
}
