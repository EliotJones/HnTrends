namespace HnDataCrawler
{
    public class HnItem
    {
        public int Id { get; set; }

        public long Time { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public int[] Kids { get; set; }
    }
}