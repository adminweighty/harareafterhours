package main

import (
	"context"
	"database/sql"
	_ "embed"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	_ "modernc.org/sqlite"
)

//go:embed migrations/sqlite/001_initial.sql
var sqliteInitialSchema string

type sqliteStore struct {
	db *sql.DB
}

func newSQLiteStore(ctx context.Context, databaseURL string) (*sqliteStore, error) {
	dsn, err := sqliteDSN(databaseURL)
	if err != nil {
		return nil, err
	}

	db, err := sql.Open("sqlite", dsn)
	if err != nil {
		return nil, fmt.Errorf("open sqlite database: %w", err)
	}
	db.SetMaxOpenConns(1)
	db.SetMaxIdleConns(1)

	store := &sqliteStore{db: db}
	if err := db.PingContext(ctx); err != nil {
		db.Close()
		return nil, fmt.Errorf("ping sqlite: %w", err)
	}
	if _, err := db.ExecContext(ctx, "PRAGMA busy_timeout = 5000"); err != nil {
		db.Close()
		return nil, fmt.Errorf("configure sqlite: %w", err)
	}
	if _, err := db.ExecContext(ctx, sqliteInitialSchema); err != nil {
		db.Close()
		return nil, fmt.Errorf("apply sqlite campaign schema: %w", err)
	}
	if err := ensureSQLitePlayerNameColumn(ctx, db); err != nil {
		db.Close()
		return nil, err
	}
	return store, nil
}

func (s *sqliteStore) close() {
	_ = s.db.Close()
}

func (s *sqliteStore) storageName() string {
	return "sqlite"
}

func (s *sqliteStore) get(ctx context.Context, profileID string) (campaignSnapshot, bool, error) {
	return loadSQLiteSnapshot(ctx, s.db, profileID)
}

func (s *sqliteStore) save(ctx context.Context, snapshot campaignSnapshot) (campaignSnapshot, error) {
	const upsert = `
		INSERT INTO campaign_saves (
				profile_id, player_name, wallet, xp, crew_trust, community_support,
			completed_levels, best_scores, last_choice, updated_at
		)
			VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'now'))
		ON CONFLICT(profile_id) DO UPDATE SET
				player_name = excluded.player_name,
				wallet = excluded.wallet,
			xp = excluded.xp,
			crew_trust = excluded.crew_trust,
			community_support = excluded.community_support,
			completed_levels = excluded.completed_levels,
			best_scores = excluded.best_scores,
			last_choice = excluded.last_choice,
			updated_at = STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'now')`

	completedLevelsJSON, err := json.Marshal(snapshot.CompletedLevels)
	if err != nil {
		return campaignSnapshot{}, fmt.Errorf("encode completed levels: %w", err)
	}
	bestScoresJSON, err := json.Marshal(snapshot.BestScores)
	if err != nil {
		return campaignSnapshot{}, fmt.Errorf("encode campaign scores: %w", err)
	}

	tx, err := s.db.BeginTx(ctx, nil)
	if err != nil {
		return campaignSnapshot{}, fmt.Errorf("begin sqlite campaign save: %w", err)
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(
		ctx,
		upsert,
		snapshot.ProfileID,
		snapshot.PlayerName,
		snapshot.Wallet,
		snapshot.XP,
		snapshot.CrewTrust,
		snapshot.CommunitySupport,
		string(completedLevelsJSON),
		string(bestScoresJSON),
		snapshot.LastChoice,
	); err != nil {
		return campaignSnapshot{}, fmt.Errorf("save sqlite campaign: %w", err)
	}

	saved, found, err := loadSQLiteSnapshot(ctx, tx, snapshot.ProfileID)
	if err != nil {
		return campaignSnapshot{}, err
	}
	if !found {
		return campaignSnapshot{}, errors.New("sqlite campaign save disappeared")
	}
	if err := tx.Commit(); err != nil {
		return campaignSnapshot{}, fmt.Errorf("commit sqlite campaign save: %w", err)
	}
	return saved, nil
}

func (s *sqliteStore) leaderboard(ctx context.Context, limit int) ([]leaderboardRecord, error) {
	const query = `
		SELECT profile_id, player_name, xp, completed_levels, best_scores, updated_at
		FROM campaign_saves`
	rows, err := s.db.QueryContext(ctx, query)
	if err != nil {
		return nil, fmt.Errorf("load sqlite leaderboard: %w", err)
	}
	defer rows.Close()
	records := make([]leaderboardRecord, 0)
	for rows.Next() {
		var record leaderboardRecord
		var completedJSON, scoresJSON, updatedAt string
		if err := rows.Scan(
			&record.ProfileID,
			&record.PlayerName,
			&record.XP,
			&completedJSON,
			&scoresJSON,
			&updatedAt,
		); err != nil {
			return nil, fmt.Errorf("scan sqlite leaderboard: %w", err)
		}
		var completed []int
		var scores map[string]int
		if err := json.Unmarshal([]byte(completedJSON), &completed); err != nil {
			return nil, fmt.Errorf("decode sqlite leaderboard missions: %w", err)
		}
		if err := json.Unmarshal([]byte(scoresJSON), &scores); err != nil {
			return nil, fmt.Errorf("decode sqlite leaderboard scores: %w", err)
		}
		record.CompletedMissions = len(completed)
		for _, score := range scores {
			record.Score += score
		}
		record.UpdatedAt, err = time.Parse(time.RFC3339Nano, updatedAt)
		if err != nil {
			return nil, fmt.Errorf("decode sqlite leaderboard update time: %w", err)
		}
		records = append(records, record)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("read sqlite leaderboard: %w", err)
	}
	sort.Slice(records, func(i, j int) bool {
		if records[i].Score != records[j].Score {
			return records[i].Score > records[j].Score
		}
		if records[i].XP != records[j].XP {
			return records[i].XP > records[j].XP
		}
		if !records[i].UpdatedAt.Equal(records[j].UpdatedAt) {
			return records[i].UpdatedAt.Before(records[j].UpdatedAt)
		}
		return records[i].ProfileID < records[j].ProfileID
	})
	if len(records) > limit {
		records = records[:limit]
	}
	return records, nil
}

type sqliteQueryer interface {
	QueryRowContext(context.Context, string, ...any) *sql.Row
}

func loadSQLiteSnapshot(
	ctx context.Context,
	queryer sqliteQueryer,
	profileID string,
) (campaignSnapshot, bool, error) {
	const query = `
			SELECT profile_id, player_name, wallet, xp, crew_trust, community_support,
		       completed_levels, best_scores, last_choice, updated_at
		FROM campaign_saves
		WHERE profile_id = ?`

	var snapshot campaignSnapshot
	var completedLevelsJSON string
	var bestScoresJSON string
	var updatedAt string
	err := queryer.QueryRowContext(ctx, query, profileID).Scan(
		&snapshot.ProfileID,
		&snapshot.PlayerName,
		&snapshot.Wallet,
		&snapshot.XP,
		&snapshot.CrewTrust,
		&snapshot.CommunitySupport,
		&completedLevelsJSON,
		&bestScoresJSON,
		&snapshot.LastChoice,
		&updatedAt,
	)
	if errors.Is(err, sql.ErrNoRows) {
		return campaignSnapshot{}, false, nil
	}
	if err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("load sqlite campaign save: %w", err)
	}
	if err := json.Unmarshal([]byte(completedLevelsJSON), &snapshot.CompletedLevels); err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("decode sqlite completed levels: %w", err)
	}
	if err := json.Unmarshal([]byte(bestScoresJSON), &snapshot.BestScores); err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("decode sqlite campaign scores: %w", err)
	}
	snapshot.UpdatedAt, err = time.Parse(time.RFC3339Nano, updatedAt)
	if err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("decode sqlite update time: %w", err)
	}
	return snapshot, true, nil
}

func ensureSQLitePlayerNameColumn(ctx context.Context, db *sql.DB) error {
	rows, err := db.QueryContext(ctx, "PRAGMA table_info(campaign_saves)")
	if err != nil {
		return fmt.Errorf("inspect sqlite campaign schema: %w", err)
	}
	found := false
	for rows.Next() {
		var cid, notNull, primaryKey int
		var name, columnType string
		var defaultValue sql.NullString
		if err := rows.Scan(&cid, &name, &columnType, &notNull, &defaultValue, &primaryKey); err != nil {
			rows.Close()
			return fmt.Errorf("read sqlite campaign schema: %w", err)
		}
		if name == "player_name" {
			found = true
		}
	}
	if err := rows.Close(); err != nil {
		return fmt.Errorf("close sqlite campaign schema: %w", err)
	}
	if found {
		return nil
	}
	if _, err := db.ExecContext(
		ctx,
		"ALTER TABLE campaign_saves ADD COLUMN player_name TEXT NOT NULL DEFAULT 'Player'",
	); err != nil {
		return fmt.Errorf("add sqlite leaderboard identity: %w", err)
	}
	return nil
}

func sqliteDSN(databaseURL string) (string, error) {
	const prefix = "sqlite://"
	trimmedURL := strings.TrimSpace(databaseURL)
	if len(trimmedURL) < len(prefix) || !strings.EqualFold(trimmedURL[:len(prefix)], prefix) {
		return "", errors.New("sqlite DATABASE_URL must start with sqlite://")
	}
	pathAndQuery := trimmedURL[len(prefix):]

	path, query, _ := strings.Cut(pathAndQuery, "?")
	if path == "" {
		return "", errors.New("sqlite DATABASE_URL must include a database path")
	}
	if path == ":memory:" {
		if query == "" {
			return ":memory:", nil
		}
		return "file::memory:?" + query, nil
	}
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		return "", fmt.Errorf("create sqlite database directory: %w", err)
	}

	dsn := "file:" + path
	if query != "" {
		dsn += "?" + query
	}
	return dsn, nil
}
