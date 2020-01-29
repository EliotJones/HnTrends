namespace HnTrends.Database
{
    public static class Schema
    {
        public const string Create = @"
CREATE TABLE IF NOT EXISTS version (id INTEGER);

CREATE TABLE IF NOT EXISTS last_write (id INTEGER);

CREATE TABLE IF NOT EXISTS date_range (first INTEGER, last INTEGER);

CREATE TABLE IF NOT EXISTS story (
    id INTEGER PRIMARY KEY,
    title TEXT NULL COLLATE NOCASE,
    url TEXT NULL COLLATE NOCASE,
    ticks INTEGER
);

CREATE INDEX ix_ticks ON story (ticks);";

        public const string VersionTable = "version";
        public const string LastWriteTable = "last_write";
        public const string DateRangeTable = "date_range";
        public const string StoryTable = "story";
    }
}
