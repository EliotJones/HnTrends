CREATE VIRTUAL TABLE IF NOT EXISTS search_target
USING FTS5(id, title, time, tokenize = "unicode61 tokenchars '#+*'");

INSERT INTO search_target
SELECT id, title, time FROM story;