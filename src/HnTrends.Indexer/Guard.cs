namespace HnTrends.Indexer
{
    using System;
    using System.IO;

    internal static class Guard
    {
        public static void CheckDirectoryValid(string directory, string propertyName)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (!Directory.Exists(directory))
            {
                throw new ArgumentException($"No directory for {propertyName} found: {directory}.", propertyName);
            }
        }
    }
}
