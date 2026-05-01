#    ╭────────────────────────────╮
#    │        Build stage         │
#    ╰────────────────────────────╯

FROM golang:1.26.1-alpine AS build
WORKDIR /app

# Copy the dependencies list
COPY go.mod go.sum ./

# Download dependencies beforehand.
# This is to avoid redownloading everytime
# part of the code changes, which would be the case
# if we don't do it beforehand in a separate layer.
RUN go mod download

# Copy the go files
COPY . .

# Disable Go functionality using external C libraries
# Then tell the compiler to target linux env even if on MacOS or Windows
RUN CGO_ENABLED=0 GOOS=linux go build -o gateway_binary ./cmd/gateway/ && \
    CGO_ENABLED=0 GOOS=linux go build -o healthcheck_binary ./cmd/healthcheck/

#    ╭────────────────────────────╮
#    │         Run stage          │
#    ╰────────────────────────────╯

FROM alpine:3.23.3
RUN apk add --no-cache curl
COPY --from=build /app/gateway_binary ./gateway_binary
COPY --from=build /app/healthcheck_binary ./healthcheck_binary

EXPOSE 8080

CMD ["./gateway_binary"]
