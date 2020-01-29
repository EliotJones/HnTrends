namespace HnTrends.Indexer
{
    using System;

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