namespace HnTrends.Database
{
    using Microsoft.Data.Sqlite;

    public interface IConnectionFactory
    {
        SqliteConnection Open();
    }
}