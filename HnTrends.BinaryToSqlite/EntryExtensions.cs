namespace HnTrends.BinaryToSqlite
{
    using System.IO;
    using Core;

    internal static class EntryExtensions
    {
        public static Entry Read(BinaryReader reader)
        {
            var result = new Entry
            {
                Id = reader.ReadInt32(),
                Title = reader.ReadString(),
                Url = reader.ReadString(),
                Time = reader.ReadInt64()
            };

            result.Date = Entry.TimeToDate(result.Time);

            return result;
        }
    }
}
