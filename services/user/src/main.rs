use std::sync::Arc;

use aide::{
    axum::{
        routing::{get, get_with, patch_with},
        ApiRouter,
    },
    openapi::{Info, OpenApi},
};
use axum::Extension;
use sqlx::{postgres::PgPoolOptions, PgPool};

mod auth;
mod dto;
mod consumer;
mod handlers;
mod repository;

#[derive(Debug, Clone)]
pub(crate) struct AppState {
    db: PgPool,
}

#[tokio::main]
async fn main() {
    let database_url = std::env::var("DATABASE_URL").expect("DATABASE_URL missing");
    let db = PgPoolOptions::new()
        .max_connections(5)
        .connect(&database_url)
        .await
        .expect("db connect failed");

    let state = Arc::new(AppState { db });
    let is_dev = std::env::var("APP_ENV").as_deref() == Ok("development");

    consumer::spawn_user_registered_consumer(state.db.clone());

    let mut api = OpenApi {
        info: Info {
            title: "User Service".into(),
            version: "1.0.0".into(),
            ..Default::default()
        },
        ..Default::default()
    };

    let mut router = ApiRouter::new()
        .api_route("/v1/hello-world", get(handlers::hello))
        .api_route("/v1/users", get_with(handlers::get_users, |t| t))
        .api_route("/v1/users/me", get_with(handlers::get_me, |t| t))
        .api_route("/v1/users/{id}", get_with(handlers::get_user, |t| t))
        .api_route("/v1/users/{id}", patch_with(handlers::patch_user, |t| t));

    if is_dev {
        router = router.route(
            "/openapi/v1.json",
            axum::routing::get(handlers::serve_openapi),
        );
    }

    let app = router.finish_api(&mut api);

    let app = if is_dev {
        app.layer(Extension(Arc::new(api))).layer(Extension(state))
    } else {
        app.layer(Extension(state))
    };

    let listener = tokio::net::TcpListener::bind("0.0.0.0:8080")
        .await
        .expect("bind failed");

    axum::serve(listener, app)
        .with_graceful_shutdown(async {
            tokio::signal::unix::signal(tokio::signal::unix::SignalKind::terminate())
                .expect("failed to install SIGTERM handler")
                .recv()
                .await;
        })
        .await
        .expect("server failed");
}
