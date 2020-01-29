namespace HnTrends.BinaryToSqlite
{
    using System;
    using System.Data.SQLite;
    using System.IO;
    using Core;
    using Database;

    public static class Program
    {
        /// <summary>
        /// Handles the one-time migration of data from the binary file format to SQLite.
        /// </summary>
        public static void Main(string[] args)
        {
            const string databaseLocation = @"C:\git\csharp\hn-reader\data";
            const string dataLocation = @"C:\git\csharp\hn-reader";

            var dbName = Path.Combine(databaseLocation, "hn-data.sqlite");

            if (File.Exists(dbName))
            {
                throw new InvalidOperationException("Database already exists! " + dbName);
            }

            using (var connection = Connector.ConnectToFile(dbName))
            {
                var command = new SQLiteCommand(Schema.Create, connection);

                command.ExecuteNonQuery();

                VersionTable.Write(1, connection);

                var backfilledFiles = Directory.GetFiles(dataLocation, "*-complete.bin");

                var trackers = new DataTrackers();

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var backfilledFile in backfilledFiles)
                    {
                        WriteFileIntoDatabase(backfilledFile, connection, trackers);
                        Console.WriteLine("Completed file: " + Path.GetFileName(backfilledFile));
                    }

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    WriteFileIntoDatabase(Path.Combine(dataLocation, "hn.bin"), connection, trackers);
                    transaction.Commit();
                }
                
                LastWriteTable.Write(trackers.MaxId, connection);
                DateRangeTable.Write(trackers.MinDate, trackers.MaxDate, connection);
            }
        }

        private static void WriteFileIntoDatabase(string filename, SQLiteConnection connection, DataTrackers trackers)
        {
            using (var file = File.OpenRead(filename))
            using (var reader = new BinaryReader(file))
            {
                while (file.Position < file.Length)
                {
                    var entry = EntryExtensions.Read(reader);

                    trackers.Update(entry);

                    StoryTable.Write(entry, connection);
                }
            }
        }

        private class DataTrackers
        {
            private DateTime? minDate;

            public DateTime MinDate => minDate.GetValueOrDefault(MaxDate);

            public DateTime MaxDate { get; private set; }

            public int MaxId { get; private set; }

            public void Update(Entry entry)
            {
                if (entry.Date < minDate || !minDate.HasValue)
                {
                    minDate = entry.Date;
                }

                if (entry.Date > MaxDate)
                {
                    MaxDate = entry.Date;
                }

                if (entry.Id > MaxId)
                {
                    MaxId = entry.Id;
                }
            }
        }
    }
}
