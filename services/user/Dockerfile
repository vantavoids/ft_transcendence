FROM rust:1.88-alpine AS chef
RUN apk add --no-cache musl-dev && cargo install cargo-chef --locked
WORKDIR /app

FROM chef AS planner
COPY Cargo.toml Cargo.lock ./
RUN mkdir src && echo 'fn main() {}' > src/main.rs
RUN cargo chef prepare --recipe-path recipe.json

FROM chef AS builder
COPY --from=planner /app/recipe.json recipe.json
RUN cargo chef cook --release --recipe-path recipe.json
COPY Cargo.toml Cargo.lock ./
COPY src/ src/
RUN cargo build --release

FROM alpine:3 AS runtime

RUN apk add --no-cache ca-certificates curl

WORKDIR /app

COPY --from=builder /app/target/release/user-service /app/app

EXPOSE 8080

CMD ["./app"]
