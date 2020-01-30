namespace HnTrends.Indexer
{
    using System.Collections.Generic;

    /// <summary>
    /// Used to interact with the Lucene index.
    /// </summary>
    public interface IIndexManager
    {
        /// <summary>
        /// Search for a given term.
        /// </summary>
        IReadOnlyList<LocatedEntry> Search(string searchTerm);

        /// <summary>
        /// Search for a given term and return the full data for each result.
        /// </summary>
        IReadOnlyList<EntryWithScore> SearchWithFullResults(string searchTerm);

        /// <summary>
        /// Update the index, call after the download task has run.
        /// </summary>
        void UpdateIndex();
    }
}