# Harare After Hours

Flutter campaign prototype based on the supplied game script. The app uses Stacked
for campaign state and has a Go development service for optional campaign-save
syncing. Local API runs use SQLite; the Docker stack continues to use PostgreSQL.

## Unity project

The editable Unity 6 project is versioned with the app in
[`unity/unity_game/`](unity/unity_game/). Open that directory in Unity Hub with
Unity `6000.3.24f1`. Git tracks `Assets`,
`Packages`, and `ProjectSettings`; Unity regenerates `Library`, `Logs`,
`UserSettings`, and platform build exports locally.

After changing Unity gameplay, refresh the embedded iOS export and framework:

```bash
zsh tool/prepare_ios_device.sh
```

The script uses the repository's `unity/unity_game/` project by default.
`UNITY_PROJECT` can still override it when diagnosing a separate checkout.

## Run the Flutter app

For the Unity game on a physical iPhone, first follow
[physical-device deployment](docs/ios-device-deployment.md). The simulator and
device Unity frameworks are different binaries even when both report `arm64`.

```bash
flutter run --dart-define=CAMPAIGN_API_URL=http://localhost:8080
```

The app remains playable offline if the API is not running. Open **Menu → Top
scores** to set a public player name, optionally enter an email identity, publish
best scores and compare rankings. Email stays in local preferences; only a one-way
hash is used as the campaign profile key. For a shared production leaderboard,
build with `CAMPAIGN_API_URL` pointing to a hosted HTTPS instance of the API.

## Run the local development API (SQLite)

```bash
cd backend
go run ./cmd/api
```

With no `DATABASE_URL`, the API creates `backend/harare_after_hours.sqlite` and
reports `{"storage":"sqlite"}` from `GET /health`. This is the default for a
localhost API run and does not require Docker.

To use another local file, set a SQLite URL:

```bash
DATABASE_URL=sqlite://./data/harare-after-hours.sqlite go run ./cmd/api
```

## Run the Docker development API (PostgreSQL)

```bash
docker compose up --build
```

The Go API waits for PostgreSQL to become healthy, applies its campaign schema, and
persists saves in the `postgres-data` Docker volume. It is not an authenticated
production service; add identity, authorization, observability, backups, and managed
database credentials before exposing it publicly.
