using Microsoft.Data.Sqlite;

namespace HnTrends.Database
{
    using System;

    public static class Connector
    {
        /// <summary>
        /// Connects to the given filename, opens the connection.
        /// </summary>
        public static SqliteConnection ConnectToFile(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var connection = new SqliteConnection($"Data Source={filename}");
            connection.Open();
            return connection;
        }
    }
}