package main

import (
	"context"
	_ "embed"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

const (
	maxCampaignValue = 100_000_000
	maxChoiceLength  = 500
	maxRequestBytes  = 1 << 20
)

var profileIDPattern = regexp.MustCompile(`^[A-Za-z0-9_-]{1,64}$`)

//go:embed migrations/001_initial.sql
var initialSchema string

type campaignSnapshot struct {
	ProfileID        string         `json:"profileId"`
	Wallet           int            `json:"wallet"`
	XP               int            `json:"xp"`
	CrewTrust        int            `json:"crewTrust"`
	CommunitySupport int            `json:"communitySupport"`
	CompletedLevels  []int          `json:"completedLevels"`
	BestScores       map[string]int `json:"bestScores"`
	LastChoice       string         `json:"lastChoice"`
	UpdatedAt        time.Time      `json:"updatedAt"`
}

type campaignRepository interface {
	get(context.Context, string) (campaignSnapshot, bool, error)
	save(context.Context, campaignSnapshot) (campaignSnapshot, error)
}

type campaignStore interface {
	campaignRepository
	close()
	storageName() string
}

type postgresStore struct {
	pool *pgxpool.Pool
}

func newPostgresStore(ctx context.Context, databaseURL string) (*postgresStore, error) {
	config, err := pgxpool.ParseConfig(databaseURL)
	if err != nil {
		return nil, fmt.Errorf("parse database url: %w", err)
	}
	config.MaxConns = 4
	config.MinConns = 1

	pool, err := pgxpool.NewWithConfig(ctx, config)
	if err != nil {
		return nil, fmt.Errorf("connect to postgres: %w", err)
	}
	store := &postgresStore{pool: pool}
	if err := store.pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, fmt.Errorf("ping postgres: %w", err)
	}
	if _, err := store.pool.Exec(ctx, initialSchema); err != nil {
		pool.Close()
		return nil, fmt.Errorf("apply campaign schema: %w", err)
	}
	return store, nil
}

func (s *postgresStore) close() {
	s.pool.Close()
}

func (s *postgresStore) storageName() string {
	return "postgres"
}

func (s *postgresStore) get(ctx context.Context, profileID string) (campaignSnapshot, bool, error) {
	const query = `
		SELECT profile_id, wallet, xp, crew_trust, community_support,
		       completed_levels, best_scores, last_choice, updated_at
		FROM campaign_saves
		WHERE profile_id = $1`

	var snapshot campaignSnapshot
	var completedLevels []int32
	var bestScoresJSON []byte
	err := s.pool.QueryRow(ctx, query, profileID).Scan(
		&snapshot.ProfileID,
		&snapshot.Wallet,
		&snapshot.XP,
		&snapshot.CrewTrust,
		&snapshot.CommunitySupport,
		&completedLevels,
		&bestScoresJSON,
		&snapshot.LastChoice,
		&snapshot.UpdatedAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return campaignSnapshot{}, false, nil
	}
	if err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("load campaign save: %w", err)
	}

	snapshot.CompletedLevels = levelsFromDatabase(completedLevels)
	snapshot.BestScores = make(map[string]int)
	if err := json.Unmarshal(bestScoresJSON, &snapshot.BestScores); err != nil {
		return campaignSnapshot{}, false, fmt.Errorf("decode campaign scores: %w", err)
	}
	return snapshot, true, nil
}

func (s *postgresStore) save(ctx context.Context, snapshot campaignSnapshot) (campaignSnapshot, error) {
	const query = `
		INSERT INTO campaign_saves (
			profile_id, wallet, xp, crew_trust, community_support,
			completed_levels, best_scores, last_choice
		)
		VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb, $8)
		ON CONFLICT (profile_id) DO UPDATE SET
			wallet = EXCLUDED.wallet,
			xp = EXCLUDED.xp,
			crew_trust = EXCLUDED.crew_trust,
			community_support = EXCLUDED.community_support,
			completed_levels = EXCLUDED.completed_levels,
			best_scores = EXCLUDED.best_scores,
			last_choice = EXCLUDED.last_choice,
			updated_at = NOW()
		RETURNING updated_at`

	bestScoresJSON, err := json.Marshal(snapshot.BestScores)
	if err != nil {
		return campaignSnapshot{}, fmt.Errorf("encode campaign scores: %w", err)
	}
	err = s.pool.QueryRow(
		ctx,
		query,
		snapshot.ProfileID,
		snapshot.Wallet,
		snapshot.XP,
		snapshot.CrewTrust,
		snapshot.CommunitySupport,
		levelsForDatabase(snapshot.CompletedLevels),
		bestScoresJSON,
		snapshot.LastChoice,
	).Scan(&snapshot.UpdatedAt)
	if err != nil {
		return campaignSnapshot{}, fmt.Errorf("save campaign: %w", err)
	}
	return cloneSnapshot(snapshot), nil
}

func levelsForDatabase(levels []int) []int32 {
	values := make([]int32, len(levels))
	for index, level := range levels {
		values[index] = int32(level)
	}
	return values
}

func levelsFromDatabase(levels []int32) []int {
	values := make([]int, len(levels))
	for index, level := range levels {
		values[index] = int(level)
	}
	return values
}

func cloneSnapshot(snapshot campaignSnapshot) campaignSnapshot {
	clone := snapshot
	clone.CompletedLevels = append([]int(nil), snapshot.CompletedLevels...)
	clone.BestScores = make(map[string]int, len(snapshot.BestScores))
	for key, value := range snapshot.BestScores {
		clone.BestScores[key] = value
	}
	return clone
}

type api struct {
	store      campaignRepository
	corsOrigin string
	storage    string
}

func (a *api) routes() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /health", a.health)
	mux.HandleFunc("GET /v1/profiles/{profileID}/campaign", a.getCampaign)
	mux.HandleFunc("PUT /v1/profiles/{profileID}/campaign", a.putCampaign)
	return a.withCORS(a.withNoStore(mux))
}

func (a *api) health(w http.ResponseWriter, r *http.Request) {
	storage := a.storage
	if storage == "" {
		storage = "memory"
	}
	writeJSON(w, http.StatusOK, map[string]string{
		"status":  "ok",
		"storage": storage,
	})
}

func (a *api) getCampaign(w http.ResponseWriter, r *http.Request) {
	profileID, err := validateProfileID(r.PathValue("profileID"))
	if err != nil {
		writeError(w, http.StatusBadRequest, err)
		return
	}

	snapshot, found, err := a.store.get(r.Context(), profileID)
	if err != nil {
		log.Printf("load campaign profile=%s: %v", profileID, err)
		writeError(w, http.StatusInternalServerError, errors.New("could not load campaign save"))
		return
	}
	if !found {
		writeError(w, http.StatusNotFound, errors.New("campaign save not found"))
		return
	}
	writeJSON(w, http.StatusOK, snapshot)
}

func (a *api) putCampaign(w http.ResponseWriter, r *http.Request) {
	profileID, err := validateProfileID(r.PathValue("profileID"))
	if err != nil {
		writeError(w, http.StatusBadRequest, err)
		return
	}

	r.Body = http.MaxBytesReader(w, r.Body, maxRequestBytes)
	defer r.Body.Close()
	decoder := json.NewDecoder(r.Body)
	decoder.DisallowUnknownFields()
	var snapshot campaignSnapshot
	if err := decoder.Decode(&snapshot); err != nil {
		writeError(w, http.StatusBadRequest, fmt.Errorf("invalid campaign payload: %w", err))
		return
	}
	if err := ensureSingleJSONValue(decoder); err != nil {
		writeError(w, http.StatusBadRequest, err)
		return
	}
	if err := validateSnapshot(&snapshot, profileID); err != nil {
		writeError(w, http.StatusBadRequest, err)
		return
	}

	saved, err := a.store.save(r.Context(), snapshot)
	if err != nil {
		log.Printf("save campaign profile=%s: %v", profileID, err)
		writeError(w, http.StatusInternalServerError, errors.New("could not persist campaign save"))
		return
	}
	writeJSON(w, http.StatusOK, saved)
}

func (a *api) withNoStore(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Cache-Control", "no-store")
		next.ServeHTTP(w, r)
	})
}

func (a *api) withCORS(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		origin := r.Header.Get("Origin")
		if origin != "" && (a.corsOrigin == "*" || a.corsOrigin == origin) {
			w.Header().Set("Access-Control-Allow-Origin", a.corsOrigin)
			w.Header().Set("Access-Control-Allow-Methods", "GET, PUT, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
			w.Header().Set("Vary", "Origin")
		}
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func validateProfileID(profileID string) (string, error) {
	if !profileIDPattern.MatchString(profileID) {
		return "", errors.New("profile id must contain 1–64 letters, numbers, underscores, or hyphens")
	}
	return profileID, nil
}

func validateSnapshot(snapshot *campaignSnapshot, pathProfileID string) error {
	if snapshot.ProfileID != "" && snapshot.ProfileID != pathProfileID {
		return errors.New("payload profile id does not match the request path")
	}
	snapshot.ProfileID = pathProfileID

	for label, value := range map[string]int{
		"wallet":           snapshot.Wallet,
		"xp":               snapshot.XP,
		"crewTrust":        snapshot.CrewTrust,
		"communitySupport": snapshot.CommunitySupport,
	} {
		if value < 0 || value > maxCampaignValue {
			return fmt.Errorf("%s must be between 0 and %d", label, maxCampaignValue)
		}
	}
	if len(snapshot.LastChoice) > maxChoiceLength {
		return fmt.Errorf("lastChoice must be %d characters or fewer", maxChoiceLength)
	}
	if len(snapshot.CompletedLevels) > 30 {
		return errors.New("completedLevels cannot contain more than 30 levels")
	}

	completed := make(map[int]struct{}, len(snapshot.CompletedLevels))
	for _, level := range snapshot.CompletedLevels {
		if level < 1 || level > 30 {
			return errors.New("completedLevels must contain levels from 1 through 30")
		}
		if _, exists := completed[level]; exists {
			return errors.New("completedLevels cannot contain duplicates")
		}
		completed[level] = struct{}{}
	}
	snapshot.CompletedLevels = snapshot.CompletedLevels[:0]
	for level := range completed {
		snapshot.CompletedLevels = append(snapshot.CompletedLevels, level)
	}
	sort.Ints(snapshot.CompletedLevels)

	if len(snapshot.BestScores) > 30 {
		return errors.New("bestScores cannot contain more than 30 entries")
	}
	for level, score := range snapshot.BestScores {
		if !validLevelKey(level) || score < 0 || score > 1000 {
			return errors.New("bestScores must use level keys 1–30 and scores 0–1000")
		}
	}
	return nil
}

func validLevelKey(value string) bool {
	level, err := strconv.Atoi(value)
	return err == nil && strconv.Itoa(level) == value && level >= 1 && level <= 30
}

func ensureSingleJSONValue(decoder *json.Decoder) error {
	var extra any
	if err := decoder.Decode(&extra); err != io.EOF {
		if err == nil {
			return errors.New("campaign payload must contain one JSON object")
		}
		return fmt.Errorf("invalid campaign payload: %w", err)
	}
	return nil
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	if err := json.NewEncoder(w).Encode(value); err != nil {
		log.Printf("write json response: %v", err)
	}
}

func writeError(w http.ResponseWriter, status int, err error) {
	writeJSON(w, status, map[string]string{"error": err.Error()})
}

func main() {
	port := valueOrDefault(os.Getenv("PORT"), "8080")
	databaseURL := valueOrDefault(
		os.Getenv("DATABASE_URL"),
		"sqlite://./harare_after_hours.sqlite",
	)
	corsOrigin := valueOrDefault(os.Getenv("CORS_ORIGIN"), "*")

	startupContext, cancel := context.WithTimeout(context.Background(), 20*time.Second)
	defer cancel()
	store, err := newCampaignStore(startupContext, databaseURL)
	if err != nil {
		log.Fatalf("initialize campaign store: %v", err)
	}
	defer store.close()

	server := &http.Server{
		Addr:              ":" + port,
		Handler:           (&api{store: store, corsOrigin: corsOrigin, storage: store.storageName()}).routes(),
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       10 * time.Second,
		WriteTimeout:      10 * time.Second,
		IdleTimeout:       60 * time.Second,
	}
	log.Printf("campaign API listening on :%s using %s", port, store.storageName())
	if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatal(err)
	}
}

func newCampaignStore(ctx context.Context, databaseURL string) (campaignStore, error) {
	url := strings.ToLower(strings.TrimSpace(databaseURL))
	switch {
	case strings.HasPrefix(url, "postgres://"), strings.HasPrefix(url, "postgresql://"):
		return newPostgresStore(ctx, databaseURL)
	case strings.HasPrefix(url, "sqlite://"):
		return newSQLiteStore(ctx, databaseURL)
	default:
		return nil, fmt.Errorf(
			"unsupported DATABASE_URL scheme; use sqlite:// or postgres://",
		)
	}
}

func valueOrDefault(value, fallback string) string {
	if strings.TrimSpace(value) == "" {
		return fallback
	}
	return value
}
