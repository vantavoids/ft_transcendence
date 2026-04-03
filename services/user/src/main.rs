use axum::{
    extract::{Path, State},
    routing::get,
    Json, Router,
};
use chrono::NaiveDateTime;
use sqlx::{postgres::PgPoolOptions, FromRow, PgPool};
use uuid::Uuid;

#[derive(Debug, FromRow, serde::Serialize)]
struct User {
    id: Uuid,
    auth_id: Uuid,
    username: String,
    discriminator: Option<String>,
    created_at: NaiveDateTime,
    updated_at: NaiveDateTime,
    deleted_at: Option<NaiveDateTime>,
}

#[derive(Clone)]
struct AppState {
    db: PgPool,
}

async fn hello() -> &'static str {
    "hello world"
}

async fn get_users(
    State(state): State<AppState>,
) -> Result<Json<Vec<User>>, (axum::http::StatusCode, String)> {
    let users = sqlx::query_as::<_, User>(
        r#"
        SELECT id, auth_id, username, discriminator, created_at, updated_at, deleted_at
        FROM users
        WHERE deleted_at IS NULL
        ORDER BY created_at DESC
        "#,
    )
    .fetch_all(&state.db)
    .await
    .map_err(|e| (axum::http::StatusCode::INTERNAL_SERVER_ERROR, e.to_string()))?;

    Ok(Json(users))
}

async fn get_user(
    Path(id): Path<Uuid>,
    State(state): State<AppState>,
) -> Result<Json<User>, (axum::http::StatusCode, String)> {
    let user = sqlx::query_as::<_, User>(
        r#"
        SELECT id, auth_id, username, discriminator, created_at, updated_at, deleted_at
        FROM users
        WHERE deleted_at IS NULL
        AND id = $1
        "#,
    )
    .bind(id)
    .fetch_optional(&state.db)
    .await
    .map_err(|e| (axum::http::StatusCode::INTERNAL_SERVER_ERROR, e.to_string()))?
    .ok_or_else(|| {
        (
            axum::http::StatusCode::NOT_FOUND,
            "user not found".to_string(),
        )
    })?;

    Ok(Json(user))
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

    let app = Router::new()
        .route("/hello-world", get(hello))
        // .route("/api/users", get(get_users))
        // .route("/api/users/:id", get(get_user))
        .with_state(state);

    let listener = tokio::net::TcpListener::bind("0.0.0.0:3000")
        .await
        .expect("bind failed");

    axum::serve(listener, app).await.expect("server failed");
}
