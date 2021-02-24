namespace HnTrends.Indexer
{
    using System;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Database;
    using Lucene.Net.Index;

    public static class Indexer
    {
        private static readonly object Lock = new object();

        public static void Index(string indexDirectory, SQLiteConnection connection, IndexWriter indexWriter)
        {
            Guard.CheckDirectoryValid(indexDirectory, nameof(indexDirectory), false);

            var lockFile = Path.Combine(indexDirectory, "index.lock");

            if (File.Exists(lockFile))
            {
                return;
            }

            lock (Lock)
            {
                var indexFile = Path.Combine(indexDirectory, "index.bin");

                var lastIndexedId = int.MinValue;

                var firstRun = !File.Exists(indexFile);

                if (!firstRun)
                {
                    if (RequiresUpdate(indexFile, out var actualLastIndexed))
                    {
                        lastIndexedId = actualLastIndexed;
                    }
                    else
                    {
                        return;
                    }
                }

                if (!LastWriteTable.TryRead(connection, out var lastWriteId) || lastIndexedId >= lastWriteId)
                {
                    Trace.WriteLine($"No indexing required, last indexed {lastIndexedId} which is equal to (or greater than) {lastWriteId}.");
                    return;
                }

                Trace.WriteLine($"Indexing from {lastIndexedId} to {lastWriteId}.");

                try
                {
                    File.WriteAllBytes(lockFile, Array.Empty<byte>());

                    var count = 1;
                    foreach (var entry in StoryTable.GetEntries(connection, lastIndexedId))
                    {
                        var document = entry.ToDocument();

                        indexWriter.AddDocument(document);

                        if (count % 1000 == 0)
                        {
                            Trace.WriteLine($"Finished indexing #{count}.");
                        }

                        lastIndexedId = entry.Id;
                        count++;
                    }

                    Trace.WriteLine($"Index complete, setting last indexed id to {lastWriteId}.");

                    File.WriteAllText(indexFile, lastIndexedId.ToString(CultureInfo.InvariantCulture));
                }
                finally
                {
                    File.Delete(lockFile);
                }
            }
        }

        private static bool RequiresUpdate(string file, out int idFrom)
        {
            idFrom = 0;

            if (new FileInfo(file).Length > 0)
            {
                var text = File.ReadAllText(file);

                idFrom = int.Parse(text, CultureInfo.InvariantCulture);

                return true;
            }

            return false;
        }
    }
}
