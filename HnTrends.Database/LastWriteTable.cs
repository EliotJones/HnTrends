namespace HnTrends.Database
{
    using System.Data.SQLite;

    public static class LastWriteTable
    {
        public static void Write(int value, SQLiteConnection connection)
        {
            var command = new SQLiteCommand($@"
DELETE FROM {Schema.LastWriteTable};
INSERT INTO {Schema.LastWriteTable} (id) VALUES (@id);", connection);

            command.Parameters.AddWithValue("id", value);

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SQLiteConnection connection, out int lastWriteId)
        {
            lastWriteId = 0;

            var command = new SQLiteCommand($@"SELECT id FROM {Schema.LastWriteTable} LIMIT 1;", connection);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    lastWriteId = reader.GetInt32(0);

                    return true;
                }
            }

            return false;
        }
    }
}