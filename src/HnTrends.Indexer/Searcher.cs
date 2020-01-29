namespace HnTrends.Indexer
{
    using System;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using Lucene.Net.Analysis;
    using Lucene.Net.QueryParsers.Classic;
    using Lucene.Net.Util;

    public static class Searcher
    {
        private static readonly Sort IdSort = new Sort(new SortField(nameof(Entry.Id), SortFieldType.INT32));

        public static IReadOnlyList<LocatedEntry> Search(SearcherManager manager,
            Analyzer analyzer,
            string searchTerm)
        {
            IndexSearcher searcher = null;

            try
            {
                manager.MaybeRefresh();

                searcher = manager.Acquire();

                var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "Title", analyzer);
                var query = queryParser.Parse(searchTerm);
                
                var searchResults = searcher.Search(query, int.MaxValue, IdSort);

                var results = new LocatedEntry[searchResults.TotalHits];

                for (var i = 0; i < searchResults.ScoreDocs.Length; i++)
                {
                    var doc = searchResults.ScoreDocs[i];
                    var item = searcher.Doc(doc.Doc);
                    var id = item.GetField(nameof(Entry.Id)).GetInt32Value();

                    if (!id.HasValue)
                    {
                        throw new InvalidOperationException($"Id did not have a value for document: {item}.");
                    }

                    var time = item.GetField(nameof(Entry.Time)).GetInt64Value();

                    if (!time.HasValue)
                    {
                        throw new InvalidOperationException($"Time did not have a value for document: {item}.");
                    }

                    var date = DateTimeOffset.FromUnixTimeSeconds(time.Value).UtcDateTime;

                    results[i] = new LocatedEntry(id.Value, date);
                }

                return results;
            }
            finally
            {
                manager.Release(searcher);
            }
        }
    }
}
