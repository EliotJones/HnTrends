namespace HnTrends.Core
{
    using System;

    public class Entry
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public long Time { get; set; }

        public DateTime Date { get; set; }
        
        public static DateTime TimeToDate(long time) => DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime;

        public static long DateToTime(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }
    }
}
