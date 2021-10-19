namespace HnTrends.Services
{
    using Database;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Options;

    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IOptions<FileLocations> fileLocationOptions;

        public ConnectionFactory(IOptions<FileLocations> fileLocationOptions)
        {
            this.fileLocationOptions = fileLocationOptions;
        }

        public SqliteConnection Open()
        {
            var connection = new SqliteConnection($"Data Source={fileLocationOptions.Value.Database};");

            connection.Open();

            return connection;
        }
    }
}
