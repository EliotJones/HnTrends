﻿namespace HnTrends.Indexer
{
    using System;
    using System.IO;

    internal static class Guard
    {
        public static void CheckDirectoryValid(string directory, string propertyName,
            bool create)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (!Directory.Exists(directory))
            {
                if (!create)
                {
                    throw new ArgumentException($"No directory for {propertyName} found: {directory}.", propertyName);
                }

                Directory.CreateDirectory(directory);
            }
        }
    }
}
