namespace HnTrends
{
    using System;
    using System.IO;

    public class Entry
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
    }
}
