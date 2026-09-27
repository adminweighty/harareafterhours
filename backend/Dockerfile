FROM golang:1.25-alpine AS build

WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY cmd ./cmd
RUN CGO_ENABLED=0 GOOS=linux go build -trimpath -ldflags="-s -w" -o /out/campaign-api ./cmd/api

FROM alpine:3.20

RUN addgroup -S campaign && adduser -S campaign -G campaign && \
    mkdir /data && chown campaign:campaign /data
COPY --from=build /out/campaign-api /campaign-api

USER campaign
WORKDIR /data
ENV PORT=8080 CORS_ORIGIN=*
EXPOSE 8080
ENTRYPOINT ["/campaign-api"]
