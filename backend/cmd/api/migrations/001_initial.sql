CREATE TABLE IF NOT EXISTS campaign_saves (
  profile_id VARCHAR(64) PRIMARY KEY,
  player_name VARCHAR(32) NOT NULL DEFAULT 'Player',
  wallet INTEGER NOT NULL CHECK (wallet BETWEEN 0 AND 100000000),
  xp INTEGER NOT NULL CHECK (xp BETWEEN 0 AND 100000000),
  crew_trust INTEGER NOT NULL CHECK (crew_trust BETWEEN 0 AND 100000000),
  community_support INTEGER NOT NULL CHECK (community_support BETWEEN 0 AND 100000000),
  completed_levels INTEGER[] NOT NULL DEFAULT ARRAY[]::INTEGER[],
  best_scores JSONB NOT NULL DEFAULT '{}'::JSONB,
  last_choice TEXT NOT NULL DEFAULT '' CHECK (char_length(last_choice) <= 500),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS campaign_saves_updated_at_idx ON campaign_saves (updated_at DESC);
