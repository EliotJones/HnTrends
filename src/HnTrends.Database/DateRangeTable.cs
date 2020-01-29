namespace HnTrends.Database
{
    using System;
    using System.Data.SQLite;
    using Core;

    public static class DateRangeTable
    {
        public static void Write(DateTime from, DateTime to, SQLiteConnection connection)
        {
            var command = new SQLiteCommand($@"
DELETE FROM {Schema.DateRangeTable};
INSERT INTO {Schema.DateRangeTable} (first, last) VALUES (@from, @to);", connection);

            command.Parameters.AddWithValue("from", Entry.DateToTime(from));
            command.Parameters.AddWithValue("to", Entry.DateToTime(to));

            command.ExecuteNonQuery();
        }

        public static bool TryRead(SQLiteConnection connection, out (DateTime from, DateTime to) range)
        {
            range = (DateTime.MinValue, DateTime.MaxValue);

            var command = new SQLiteCommand($@"SELECT first, last FROM {Schema.DateRangeTable} LIMIT 1;", connection);

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