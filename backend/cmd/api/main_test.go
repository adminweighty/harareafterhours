package main

import (
	"bytes"
	"context"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"sync"
	"testing"
	"time"
)

func TestCampaignRoundTripUsesRepository(t *testing.T) {
	server := httptest.NewServer((&api{store: newMemoryStore(), corsOrigin: "*"}).routes())
	defer server.Close()

	payload := campaignSnapshot{
		ProfileID:        "tari-demo",
		Wallet:           1200,
		XP:               225,
		CrewTrust:        1,
		CommunitySupport: 1,
		CompletedLevels:  []int{2, 1},
		BestScores:       map[string]int{"1": 980, "2": 880},
		LastChoice:       "Return it directly",
	}
	body, err := json.Marshal(payload)
	if err != nil {
		t.Fatalf("marshal payload: %v", err)
	}

	response, err := http.DefaultClient.Do(newRequest(t, http.MethodPut, server.URL+"/v1/profiles/tari-demo/campaign", body))
	if err != nil {
		t.Fatalf("put campaign: %v", err)
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		t.Fatalf("put status = %d", response.StatusCode)
	}

	response, err = http.Get(server.URL + "/v1/profiles/tari-demo/campaign")
	if err != nil {
		t.Fatalf("get campaign: %v", err)
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		t.Fatalf("get status = %d", response.StatusCode)
	}
	var saved campaignSnapshot
	if err := json.NewDecoder(response.Body).Decode(&saved); err != nil {
		t.Fatalf("decode campaign: %v", err)
	}
	if saved.Wallet != 1200 || len(saved.CompletedLevels) != 2 || saved.CompletedLevels[0] != 1 || saved.UpdatedAt.IsZero() {
		t.Fatalf("unexpected saved campaign: %#v", saved)
	}
}

func TestCampaignRejectsInvalidLevel(t *testing.T) {
	server := httptest.NewServer((&api{store: newMemoryStore(), corsOrigin: "*"}).routes())
	defer server.Close()

	payload := []byte(`{"profileId":"tari-demo","wallet":500,"xp":0,"crewTrust":0,"communitySupport":0,"completedLevels":[31],"bestScores":{},"lastChoice":""}`)
	response, err := http.DefaultClient.Do(newRequest(t, http.MethodPut, server.URL+"/v1/profiles/tari-demo/campaign", payload))
	if err != nil {
		t.Fatalf("put invalid campaign: %v", err)
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusBadRequest {
		t.Fatalf("status = %d, want %d", response.StatusCode, http.StatusBadRequest)
	}
}

func TestSQLiteStorePersistsCampaigns(t *testing.T) {
	store, err := newSQLiteStore(context.Background(), "sqlite://:memory:")
	if err != nil {
		t.Fatalf("new sqlite store: %v", err)
	}
	defer store.close()

	want := campaignSnapshot{
		ProfileID:        "local-player",
		Wallet:           900,
		XP:               180,
		CrewTrust:        2,
		CommunitySupport: 3,
		CompletedLevels:  []int{1, 2},
		BestScores:       map[string]int{"1": 920, "2": 810},
		LastChoice:       "Keep the parcel moving",
	}
	saved, err := store.save(context.Background(), want)
	if err != nil {
		t.Fatalf("save sqlite campaign: %v", err)
	}
	if saved.UpdatedAt.IsZero() {
		t.Fatal("sqlite save did not set updatedAt")
	}

	got, found, err := store.get(context.Background(), want.ProfileID)
	if err != nil {
		t.Fatalf("get sqlite campaign: %v", err)
	}
	if !found {
		t.Fatal("sqlite campaign was not found")
	}
	if got.Wallet != want.Wallet || got.XP != want.XP ||
		got.CompletedLevels[1] != 2 || got.BestScores["1"] != 920 {
		t.Fatalf("unexpected sqlite campaign: %#v", got)
	}
}

func TestNewCampaignStoreUsesSQLiteURL(t *testing.T) {
	store, err := newCampaignStore(context.Background(), "sqlite://:memory:")
	if err != nil {
		t.Fatalf("new campaign store: %v", err)
	}
	defer store.close()

	if store.storageName() != "sqlite" {
		t.Fatalf("storage = %q, want sqlite", store.storageName())
	}
}

type memoryStore struct {
	mu        sync.RWMutex
	snapshots map[string]campaignSnapshot
}

func newMemoryStore() *memoryStore {
	return &memoryStore{snapshots: make(map[string]campaignSnapshot)}
}

func (s *memoryStore) get(_ context.Context, profileID string) (campaignSnapshot, bool, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	snapshot, found := s.snapshots[profileID]
	return cloneSnapshot(snapshot), found, nil
}

func (s *memoryStore) save(_ context.Context, snapshot campaignSnapshot) (campaignSnapshot, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	snapshot.UpdatedAt = time.Now().UTC()
	s.snapshots[snapshot.ProfileID] = cloneSnapshot(snapshot)
	return cloneSnapshot(snapshot), nil
}

func newRequest(t *testing.T, method, url string, body []byte) *http.Request {
	t.Helper()
	request, err := http.NewRequest(method, url, bytes.NewReader(body))
	if err != nil {
		t.Fatalf("new request: %v", err)
	}
	request.Header.Set("Content-Type", "application/json")
	return request
}
