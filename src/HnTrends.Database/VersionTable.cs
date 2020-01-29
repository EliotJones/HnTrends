namespace HnTrends.Database
{
    using System.Data.SQLite;

    public static class VersionTable
    {
        public static void Write(int value, SQLiteConnection connection)
        {
            var command = new SQLiteCommand($@"
DELETE FROM {Schema.VersionTable};
INSERT INTO {Schema.VersionTable} (id) VALUES (@id);", connection);

            command.Parameters.AddWithValue("id", value);

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SQLiteConnection connection, out int version)
        {
            version = 0;

            var command = new SQLiteCommand($@"SELECT id FROM {Schema.VersionTable} LIMIT 1;", connection);

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