namespace HnTrends.Indexer
{
    using Core;

    /// <summary>
    /// Represents an entry with an associated score from Lucene.
    /// </summary>
    public class EntryWithScore : Entry
    {
        /// <summary>
        /// The score associated with this entry in the Lucene search result.
        /// </summary>
        public double Score { get; set; }
    }
}