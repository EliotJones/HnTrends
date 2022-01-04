using Microsoft.Data.Sqlite;

namespace HnTrends.Database
{
    using System;
    using System.Collections.Generic;
    using Core;

    public static class StoryTable
    {
        public static void Write(Entry entry, SqliteConnection connection, SqliteTransaction transaction)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var command = new SqliteCommand(@"INSERT OR IGNORE INTO story(id, title, url, time) 
VALUES(@id, @title, @url, @time);

INSERT INTO search_target(id, title, time) VALUES (@id, @title, @time);", 
                connection, transaction);

            command.Parameters.AddWithValue("id", entry.Id);
            command.Parameters.AddWithValue("title", entry.Title);
            command.Parameters.AddWithValue("url", entry.Url);
            command.Parameters.AddWithValue("time", entry.Time);

            command.ExecuteNonQuery();
        }

        public static IEnumerable<Entry> GetEntries(SqliteConnection connection,
            int minId = 0)
        {
            var command = new SqliteCommand($@"SELECT id, title, url, time FROM {Schema.StoryTable}
 WHERE id >= @id;",
                connection);

            command.Parameters.AddWithValue("id", minId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entry = new Entry
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Url = reader.GetString(2),
                        Time = reader.GetInt64(3)
                    };

                    entry.Date = Entry.TimeToDate(entry.Time);

                    yield return entry;
                }
            }
        }

        public static int GetCount(SqliteConnection connection)
        {
            var command = new SqliteCommand($@"SELECT COUNT(*) FROM {Schema.StoryTable};", connection);

            return (int)(long)command.ExecuteScalar();
        }
    }
}