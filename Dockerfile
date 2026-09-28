# syntax=docker/dockerfile:1.7

FROM golang:1.25-alpine AS build

ARG TARGETOS=linux
ARG TARGETARCH=amd64

WORKDIR /src
COPY backend/go.mod backend/go.sum ./
RUN go mod download
COPY backend/cmd ./cmd
RUN CGO_ENABLED=0 GOOS=${TARGETOS} GOARCH=${TARGETARCH} \
    go build -trimpath -ldflags="-s -w" -o /out/campaign-api ./cmd/api

FROM alpine:3.22

RUN apk add --no-cache ca-certificates tzdata && \
    addgroup -S campaign && \
    adduser -S campaign -G campaign && \
    mkdir /data && \
    chown campaign:campaign /data

COPY --from=build --chown=campaign:campaign /out/campaign-api /campaign-api

USER campaign
WORKDIR /data

# Sevalla replaces PORT at runtime. These values keep local runs convenient.
ENV PORT=8080 \
    CORS_ORIGIN=*

EXPOSE 8080
STOPSIGNAL SIGTERM
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -q -O /dev/null "http://127.0.0.1:${PORT:-8080}/health" || exit 1

ENTRYPOINT ["/campaign-api"]
