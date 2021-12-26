using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HouseofCat.Utilities.File
{
    public static class DirectoryEnumerators
    {
        public static IEnumerable<string> EnumerateDirectories(
            string parentDirectory,
            string searchPattern,
            SearchOption searchOpt)
        {
            try
            {
                var directories = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    directories = Directory
                        .EnumerateDirectories(parentDirectory)
                        .SelectMany(x => EnumerateDirectories(x, searchPattern, searchOpt));
                }
                return directories.Concat(Directory.EnumerateDirectories(parentDirectory, searchPattern));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static IEnumerable<string> EnumerateDirectoriesInParallel(string parentDirectory, string searchPattern,
            SearchOption searchOpt)
        {
            try
            {
                var directories = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    directories = Directory
                        .EnumerateDirectories(parentDirectory)
                        .AsParallel()
                        .SelectMany(x => EnumerateDirectories(x, searchPattern, searchOpt));
                }
                return directories.Concat(Directory.EnumerateDirectories(parentDirectory, searchPattern));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}
