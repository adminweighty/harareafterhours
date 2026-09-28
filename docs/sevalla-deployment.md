# Deploy the campaign API to Sevalla

The root `Dockerfile` builds only the Go campaign API. It listens on the
`PORT` supplied by Sevalla, runs as a non-root user, and exposes `GET /health`
for rollout checks. The root `.dockerignore` keeps Flutter and Unity assets out
of the container build context.

## 1. Create the application

1. Push this repository and connect it under **Sevalla → Applications → Add
   application**.
2. Select the repository and production branch.
3. Choose the **Dockerfile** build strategy and set the Dockerfile path to
   `Dockerfile`.
4. Leave the start command empty. The image starts `/campaign-api` itself.
5. Create a **Web** process. Use port `8080` in the networking screen; Sevalla
   also injects the matching `PORT` value at runtime.
6. Set the health-check path to `/health`.

The successful health response is:

```json
{"status":"ok","storage":"postgres"}
```

## 2. Add PostgreSQL

Create a PostgreSQL database in the same Sevalla region as the application.
Add an internal connection between the database and application, then provide
the internal connection URI as this runtime environment variable:

```text
DATABASE_URL=postgresql://USER:PASSWORD@INTERNAL_HOST:PORT/DATABASE?sslmode=disable
```

Use the connection URI shown by Sevalla and URL-encode special characters in
the username or password. The API applies its campaign and leaderboard schema
at startup. Do not use the container's SQLite fallback for a production
leaderboard because application containers can be replaced during deployments.

Set this runtime variable as well:

```text
CORS_ORIGIN=*
```

Native iOS and Android requests do not depend on browser CORS. If a web client
is added later, replace `*` with that site's exact HTTPS origin.

## 3. Verify the deployment

After Sevalla assigns the public domain, open:

```text
https://YOUR-SEVALLA-DOMAIN/health
https://YOUR-SEVALLA-DOMAIN/v1/leaderboard?limit=10
```

The first request must report `postgres`. An empty leaderboard should return
`{"entries":[]}` until a player publishes a score.

## 4. Connect release builds

Add the public HTTPS address to the ignored `.admob.env` used by the release
scripts:

```text
CAMPAIGN_API_URL=https://YOUR-SEVALLA-DOMAIN
```

Then rebuild Android and iOS. The production scripts pass this address into the
Flutter app, allowing all installed copies to publish to the same leaderboard.
