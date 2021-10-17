using Microsoft.Data.Sqlite;

namespace HnTrends.Database
{
    using System;
    using Core;

    public static class DateRangeTable
    {
        public static void Write(DateTime from, DateTime to, SqliteConnection connection, SqliteTransaction transaction)
        {
            var command = connection.CreateCommand();

            command.CommandText = $@"
DELETE FROM {Schema.DateRangeTable};
INSERT INTO {Schema.DateRangeTable} (first, last) VALUES (@from, @to);";
            command.Transaction = transaction;

            command.Parameters.AddWithValue("from", Entry.DateToTime(from));
            command.Parameters.AddWithValue("to", Entry.DateToTime(to));

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SqliteConnection connection, out (DateTime from, DateTime to) range)
        {
            range = (DateTime.MinValue, DateTime.MaxValue);

            var command = new SqliteCommand($@"SELECT first, last FROM {Schema.DateRangeTable} LIMIT 1;", connection);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var from = reader.GetInt64(0);
                    var to = reader.GetInt64(1);

                    range = (Entry.TimeToDate(from), Entry.TimeToDate(to));

                    return true;
                }
            }

            return false;
        }
    }
}