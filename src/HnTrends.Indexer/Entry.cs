namespace HnTrends.Indexer
{
    using System;
    using System.IO;
    using Lucene.Net.Documents;

    internal class Entry
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public long Time { get; set; }

        public DateTime Date { get; set; }

        public static Entry Read(BinaryReader reader)
        {
            var result = new Entry
            {
                Id = reader.ReadInt32(),
                Title = reader.ReadString(),
                Url = reader.ReadString(),
                Time = reader.ReadInt64()
            };

            result.Date = DateTimeOffset.FromUnixTimeSeconds(result.Time).UtcDateTime;

            return result;
        }

        public Document ToDocument()
        {
            var doc = new Document
            {
                new Int32Field(nameof(Id), Id, Field.Store.YES),
                new TextField(nameof(Title), Title, Field.Store.YES),
                new TextField(nameof(Url), Url, Field.Store.YES),
                new Int64Field(nameof(Time), Time, Field.Store.YES)
            };


            return doc;
        }
    }
}
