FROM golang:1.27 AS build
WORKDIR /src

# Copy the module files first so the download layer is cached until a dependency changes.
COPY go.mod go.sum ./
RUN go mod download

COPY . .
# The SQLite driver needs CGO; the Debian-based builder has gcc.
RUN CGO_ENABLED=1 go build -trimpath -o /out/order-service ./cmd/api

# Same glibc family as the builder, so the CGO binary runs here.
FROM debian:bookworm-slim AS final
COPY --from=build /out/order-service /order-service

ENV HTTP_ADDR=:8080
ENV DB_PATH=/data/order.db

# SQLite file lives on a volume; the non-root app user must own it.
RUN useradd --system --uid 10001 app && mkdir /data && chown app:app /data
VOLUME /data
EXPOSE 8080
USER app
ENTRYPOINT ["/order-service"]
