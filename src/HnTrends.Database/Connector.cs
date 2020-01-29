namespace HnTrends.Database
{
    using System;
    using System.Data.SQLite;

    public static class Connector
    {
        /// <summary>
        /// Connects to the given filename, opens the connection.
        /// </summary>
        public static SQLiteConnection ConnectToFile(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var connection = new SQLiteConnection($"Data Source={filename}");
            return connection.OpenAndReturn();
        }
    }
}