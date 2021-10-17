using Microsoft.Data.Sqlite;

namespace HnTrends.Database
{
    public static class LastWriteTable
    {
        public static void Write(int value, SqliteConnection connection, SqliteTransaction transaction)
        {
            var command = new SqliteCommand($@"
DELETE FROM {Schema.LastWriteTable};
INSERT INTO {Schema.LastWriteTable} (id) VALUES (@id);", connection, transaction);

            command.Parameters.AddWithValue("id", value);

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SqliteConnection connection, out int lastWriteId)
        {
            lastWriteId = 0;

            var command = new SqliteCommand($@"SELECT id FROM {Schema.LastWriteTable} LIMIT 1;", connection);

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