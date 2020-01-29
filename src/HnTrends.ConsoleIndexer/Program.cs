namespace HnTrends.ConsoleIndexer
{
    using System.Diagnostics;
    using Database;
    using Indexer;

    public static class Program
    {
        public static void Main(string[] args)
        {
            const string sqliteFile = @"C:\git\csharp\hn-reader\data\hn-data.sqlite";
            const string indexDirectory = @"C:\git\csharp\hn-reader\index";

            Trace.Listeners.Add(MyConsoleListener.Instance);

            using (var connection = Connector.ConnectToFile(sqliteFile))
            using (var manager = new IndexManager(indexDirectory, connection))
            {
                manager.UpdateIndex();
            }
        }
    }
}
