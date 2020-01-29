namespace HnTrends.Indexer
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

    public class IndexManager : IIndexManager, IDisposable
    {
        private readonly string dataDirectory;
        private readonly string indexDirectory;

        private readonly Analyzer analyzer;
        private readonly IndexWriter writer;
        private readonly SearcherManager searcherManager;

        public IndexManager(string dataDirectory, string indexDirectory)
        {
            Guard.CheckDirectoryValid(dataDirectory, nameof(dataDirectory));
            Guard.CheckDirectoryValid(indexDirectory, nameof(indexDirectory));

            this.dataDirectory = dataDirectory;
            this.indexDirectory = indexDirectory;

            var directory = FSDirectory.Open(indexDirectory);

            var standardAnalzyer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            var writerConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, standardAnalzyer);

            analyzer = standardAnalzyer;
            writer = new IndexWriter(directory, writerConfig);
            searcherManager = new SearcherManager(writer, false, new SearcherFactory());
        }

        public IReadOnlyList<LocatedEntry> Search(string searchTerm)
        {
            var results = Searcher.Search(searcherManager, analyzer, searchTerm);

            return results;
        }

        public void UpdateIndex()
        {
            Indexer.Index(dataDirectory, indexDirectory, writer);
        }

        public void Dispose()
        {
            writer?.Dispose();
        }
    }

    public struct LocatedEntry
    {
        public int Id { get; }

        public DateTime Date { get; }

        public LocatedEntry(int id, DateTime date)
        {
            Id = id;
            Date = date;
        }
    }
}
