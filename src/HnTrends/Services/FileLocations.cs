namespace HnTrends.Services
{
    /// <summary>
    /// The corresponding setting values from appsettings.json or another source.
    /// </summary>
    public class FileLocations
    {
        /// <summary>
        /// SQLite database file, full path.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Lucene index directory path.
        /// </summary>
        public string Index { get; set; }
    }
}
