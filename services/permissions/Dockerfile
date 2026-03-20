# ---- Build stage ----

FROM golang:1.26.1-alpine3.23 AS build

WORKDIR /app

# Copy the dependencies list
COPY go.mod go.sum ./

# Download dependencies beforehand.
# This is to avoid redownloading them everytime
# part of the code changes, which would be the case
# if we don't do it beforehand in a separate layer.
RUN go mod download

# Copy the go files
COPY ./cmd/ ./cmd/

# Disable Go functionality using external C libraries
# Then tell the compiler to target linux env even if on MacOS or Windows
RUN CGO_ENABLED=0 GOOS=linux go build -o /permission-app ./cmd/server/

# ---------------------



# ---- Run stage ----

FROM alpine:3.23.3

# Fetch the binary from the build stage
COPY --from=build /permission-app /permission-app

EXPOSE 1323

CMD ["/permission-app"]

# -------------------
