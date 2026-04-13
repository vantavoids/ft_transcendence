FROM rust:latest AS builder

WORKDIR /app

COPY Cargo.toml Cargo.lock ./
RUN mkdir src && echo 'fn main() {}' > src/main.rs && \
    cargo build --release && \
    rm -f target/release/deps/hello_world*

COPY src/ src/
RUN cargo build --release

FROM debian:bookworm-slim

RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=builder /app/target/release/hello_world /app/app

CMD ["./app"]
