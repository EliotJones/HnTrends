namespace HnTrends.Database
{
    using System;
    using System.Data.SQLite;
    using Core;

    public static class StoryTable
    {
        public static void Write(Entry entry, SQLiteConnection connection)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var command = new SQLiteCommand(@"INSERT OR IGNORE INTO story(id, title, url, ticks) 
VALUES(@id, @title, @url, @ticks);", 
                connection);

            command.Parameters.AddWithValue("id", entry.Id);
            command.Parameters.AddWithValue("title", entry.Title);
            command.Parameters.AddWithValue("url", entry.Url);
            command.Parameters.AddWithValue("ticks", entry.Date.Ticks);

            command.ExecuteNonQuery();
        }
    }
}