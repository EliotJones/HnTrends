namespace HnTrends.Indexer
{
    using Core;
    using Lucene.Net.Documents;

    internal static class EntryExtensions
    {
        public static Document ToDocument(this Entry entry)
        {
            var doc = new Document
            {
                new Int32Field(nameof(entry.Id), entry.Id, Field.Store.YES),
                new TextField(nameof(entry.Title), entry.Title, Field.Store.YES),
                new TextField(nameof(entry.Url), entry.Url, Field.Store.YES),
                new Int64Field(nameof(entry.Time), entry.Time, Field.Store.YES)
            };

            return doc;
        }
    }
}
