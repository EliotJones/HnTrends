using Microsoft.Data.Sqlite;

namespace HnTrends.Database
{
    public static class VersionTable
    {
        public static void Write(int value, SqliteConnection connection)
        {
            var command = new SqliteCommand($@"
DELETE FROM {Schema.VersionTable};
INSERT INTO {Schema.VersionTable} (id) VALUES (@id);", connection);

            command.Parameters.AddWithValue("id", value);

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SqliteConnection connection, out int version)
        {
            version = 0;

            var command = new SqliteCommand($@"SELECT id FROM {Schema.VersionTable} LIMIT 1;", connection);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    version = reader.GetInt32(0);

                    return true;
                }
            }

            return false;
        }

    }
}