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
    using System.Data.SQLite;

    public class IndexManager : IIndexManager, IDisposable
    {
        private readonly string indexDirectory;
        private readonly SQLiteConnection connection;

        private readonly Analyzer analyzer;
        private readonly IndexWriter writer;
        private readonly SearcherManager searcherManager;

        public IndexManager(string indexDirectory, SQLiteConnection connection)
        {
            Guard.CheckDirectoryValid(indexDirectory, nameof(indexDirectory), true);

            this.indexDirectory = indexDirectory;
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

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
            Indexer.Index(indexDirectory, connection, writer);
        }

        public void Dispose()
        {
            writer?.Dispose();
        }
    }
}
