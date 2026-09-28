CREATE TABLE IF NOT EXISTS campaign_saves (
  profile_id TEXT PRIMARY KEY,
  player_name TEXT NOT NULL DEFAULT 'Player',
  wallet INTEGER NOT NULL CHECK (wallet BETWEEN 0 AND 100000000),
  xp INTEGER NOT NULL CHECK (xp BETWEEN 0 AND 100000000),
  crew_trust INTEGER NOT NULL CHECK (crew_trust BETWEEN 0 AND 100000000),
  community_support INTEGER NOT NULL CHECK (community_support BETWEEN 0 AND 100000000),
  completed_levels TEXT NOT NULL DEFAULT '[]',
  best_scores TEXT NOT NULL DEFAULT '{}',
  last_choice TEXT NOT NULL DEFAULT '' CHECK (length(last_choice) <= 500),
  updated_at TEXT NOT NULL DEFAULT (STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE INDEX IF NOT EXISTS campaign_saves_updated_at_idx ON campaign_saves (updated_at DESC);
