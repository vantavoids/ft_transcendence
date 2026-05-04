mod models;

use aide::{
    axum::{routing::get_with, ApiRouter},
    openapi::{Info, OpenApi},
};
use axum::{extract::{Path, State}, http::StatusCode, routing::get, Extension, Json};
use models::UserProfile;
use sqlx::{postgres::PgPoolOptions, PgPool};
use std::sync::Arc;

#[allow(dead_code)]
#[derive(Clone)]
struct AppState {
    db: PgPool,
}

async fn hello() -> &'static str {
    "tu compiles hein"
}

async fn list_users(State(state): State<AppState>) -> Result<Json<Vec<UserProfile>>, StatusCode> {
    let users = sqlx::query_as::<_, UserProfile>(
        r#"
        SELECT id, username, display_name, avatar_url, banner_url, status, last_seen_at, bio, created_at, updated_at
        FROM users_profile
        ORDER BY created_at ASC
        "#,
    )
    .fetch_all(&state.db)
    .await
    .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;

    Ok(Json(users))
}

async fn get_user(
    State(state): State<AppState>,
    Path(id): Path<i64>,
) -> Result<Json<UserProfile>, StatusCode> {
    let user = sqlx::query_as::<_, UserProfile>(
        r#"
        SELECT id, username, display_name, avatar_url, banner_url, status, last_seen_at, bio, created_at, updated_at
        FROM users_profile
        WHERE id = $1
        "#,
    )
    .bind(id)
    .fetch_optional(&state.db)
    .await
    .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?
    .ok_or(StatusCode::NOT_FOUND)?;

    Ok(Json(user))
}

async fn serve_openapi(Extension(api): Extension<Arc<OpenApi>>) -> Json<OpenApi> {
    Json((*api).clone())
}

#[tokio::main]
async fn main() {
    let database_url = std::env::var("DATABASE_URL").expect("DATABASE_URL missing");
    let db = PgPoolOptions::new()
        .max_connections(5)
        .connect(&database_url)
        .await
        .expect("db connect failed");

    let state = AppState { db };

    let is_dev = std::env::var("APP_ENV").as_deref() == Ok("development");

    let mut api = OpenApi {
        info: Info {
            title: "User Service".into(),
            version: "1.0.0".into(),
            ..Default::default()
        },
        ..Default::default()
    };

    let mut router = ApiRouter::new()
        .api_route("/v1/hello-world", get_with(hello, |t| t))
        .route("/v1/users", get(list_users))
        .route("/v1/users/:id", get(get_user))
        .with_state(state);

    if is_dev {
        router = router.route("/openapi/v1.json", get(serve_openapi));
    }

    let app = router.finish_api(&mut api);

    let app = if is_dev {
        app.layer(Extension(Arc::new(api)))
    } else {
        app
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
