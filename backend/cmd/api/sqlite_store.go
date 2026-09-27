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
			profile_id, wallet, xp, crew_trust, community_support,
			completed_levels, best_scores, last_choice, updated_at
		)
		VALUES (?, ?, ?, ?, ?, ?, ?, ?, STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'now'))
		ON CONFLICT(profile_id) DO UPDATE SET
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

type sqliteQueryer interface {
	QueryRowContext(context.Context, string, ...any) *sql.Row
}

func loadSQLiteSnapshot(
	ctx context.Context,
	queryer sqliteQueryer,
	profileID string,
) (campaignSnapshot, bool, error) {
	const query = `
		SELECT profile_id, wallet, xp, crew_trust, community_support,
		       completed_levels, best_scores, last_choice, updated_at
		FROM campaign_saves
		WHERE profile_id = ?`

	var snapshot campaignSnapshot
	var completedLevelsJSON string
	var bestScoresJSON string
	var updatedAt string
	err := queryer.QueryRowContext(ctx, query, profileID).Scan(
		&snapshot.ProfileID,
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
