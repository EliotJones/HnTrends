namespace HnTrends.Indexer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Lucene.Net.Index;
    using Directory = System.IO.Directory;

    public static class Indexer
    {
        private static readonly object Lock = new object();

        public static void Index(string dataDirectory, string indexDirectory, IndexWriter indexWriter)
        {
            Guard.CheckDirectoryValid(indexDirectory, nameof(indexDirectory));
            Guard.CheckDirectoryValid(dataDirectory, nameof(dataDirectory));

            var lockFile = Path.Combine(indexDirectory, "index.lock");

            if (File.Exists(lockFile))
            {
                return;
            }

            lock (Lock)
            {
                var indexFile = Path.Combine(indexDirectory, "index.bin");

                var firstRun = !File.Exists(indexFile);

                var requiresUpdate = firstRun;

                int? minId = null;
                if (!firstRun)
                {
                    if (RequiresUpdate(indexFile, out var idFrom))
                    {
                        requiresUpdate = true;
                        minId = idFrom;
                    }
                }

                if (!requiresUpdate)
                {
                    return;
                }

                try
                {
                    File.WriteAllBytes(lockFile, Array.Empty<byte>());

                    var maxIdEncountered = minId.GetValueOrDefault();
                    
                    //create an index writer
                    foreach (var indexable in GetFilesToIndex(dataDirectory))
                    {
                        var filename = Path.GetFileNameWithoutExtension(indexable);
                        var numberPart = filename.Substring(0, filename.IndexOf('h'));
                        var num = int.Parse(numberPart);

                        if (num + 250_000 <= maxIdEncountered)
                        {
                            continue;
                        }

                        using (var file = File.OpenRead(indexable))
                        using (var reader = new BinaryReader(file))
                        {
                            while (file.Position < file.Length)
                            {
                                var entry = Entry.Read(reader);

                                if (minId.HasValue && entry.Id <= minId.Value)
                                {
                                    continue;
                                }

                                if (entry.Id > maxIdEncountered)
                                {
                                    maxIdEncountered = entry.Id;
                                }

                                var document = entry.ToDocument();

                                indexWriter.AddDocument(document);
                            }
                        }
                    }

                    File.WriteAllText(indexFile, maxIdEncountered.ToString(CultureInfo.InvariantCulture));
                }
                finally
                {
                    File.Delete(lockFile);
                }
            }
        }

        private static IEnumerable<string> GetFilesToIndex(string directory)
        {
            var files = Directory.GetFiles(directory, "*-complete.bin");

            foreach (var file in files)
            {
                yield return file;
            }

            var main = Path.Combine(directory, "hn.bin");

            if (File.Exists(main))
            {
                // yield return main;
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
