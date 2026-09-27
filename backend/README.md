# Campaign API

The Go service stores campaign snapshots transactionally and keeps the Flutter
client's `GET`/`PUT` contract unchanged. Its storage is selected by `DATABASE_URL`:

- omitted: local SQLite at `sqlite://./harare_after_hours.sqlite`
- `sqlite://./path/to/campaign.sqlite`: a chosen local SQLite file
- `postgres://...` or `postgresql://...`: PostgreSQL

The Docker Compose service supplies a PostgreSQL URL. A direct localhost run uses
SQLite by default and creates the database file in the current directory.
The Docker image has a writable `/data` working directory, so it also falls back to
SQLite safely when run without a PostgreSQL URL; mount `/data` if that file should
survive container replacement.

It deliberately has no authentication, payment processing, or leaderboard logic.
Use it for local development only until an authenticated profile system is designed.

## Endpoints

- `GET /health`
- `GET /v1/profiles/{profileId}/campaign`
- `PUT /v1/profiles/{profileId}/campaign`

The API validates profile IDs, mission levels, scores, bounded counters, and payload
size. PostgreSQL stores levels as an integer array and scores as JSONB; SQLite stores
both values as JSON text.

For a local browser build without Docker, run `go run ./cmd/api` from this directory.
For the PostgreSQL stack, run `docker compose up --build` from the project root. The
default CORS setting is intentionally permissive for development. Set `CORS_ORIGIN`
to a specific trusted origin and use a secret, URL-encoded `POSTGRES_PASSWORD` before
any hosted deployment.
