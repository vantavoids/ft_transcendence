use aide::{
    axum::{routing::get_with, ApiRouter},
    openapi::{Info, OpenApi},
};
use axum::{http::StatusCode, routing::get, Extension, Json};
use chrono::NaiveDateTime;
use schemars::JsonSchema;
use sqlx::{postgres::PgPoolOptions, FromRow, PgPool};
use std::sync::Arc;
use uuid::Uuid;

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
struct User {
    id: Uuid,
    auth_id: Uuid,
    username: String,
    discriminator: Option<String>,
    created_at: NaiveDateTime,
    updated_at: NaiveDateTime,
    deleted_at: Option<NaiveDateTime>,
}

#[allow(dead_code)]
#[derive(Clone)]
struct AppState {
    db: PgPool,
}

async fn hello() -> &'static str {
    "tu compiles hein"
}

async fn serve_openapi(Extension(api): Extension<Arc<OpenApi>>) -> Json<OpenApi> {
    Json((*api).clone())
}

#[derive(serde::Serialize)]
struct HealthCheckEntry {
    name: &'static str,
    status: &'static str,
    description: Option<String>,
}

#[derive(serde::Serialize)]
struct HealthReport {
    status: &'static str,
    checks: Vec<HealthCheckEntry>,
}

async fn healthz(Extension(db): Extension<PgPool>) -> (StatusCode, Json<HealthReport>) {
    let (code, check) = match sqlx::query("SELECT 1").execute(&db).await {
        Ok(_) => (
            StatusCode::OK,
            HealthCheckEntry { name: "postgres", status: "Healthy", description: None },
        ),
        Err(e) => (
            StatusCode::SERVICE_UNAVAILABLE,
            HealthCheckEntry {
                name: "postgres",
                status: "Unhealthy",
                description: Some(format!("postgres unreachable: {e}")),
            },
        ),
    };

    let status = if code == StatusCode::OK { "Healthy" } else { "Unhealthy" };
    (code, Json(HealthReport { status, checks: vec![check] }))
}

#[tokio::main]
async fn main() {
    let database_url = std::env::var("DATABASE_URL").expect("DATABASE_URL missing");
    let db = PgPoolOptions::new()
        .max_connections(5)
        .connect(&database_url)
        .await
        .expect("db connect failed");

    let _state = AppState { db: db.clone() };

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
        .route("/healthz", get(healthz));

    if is_dev {
        router = router.route("/openapi/v1.json", get(serve_openapi));
    }

    let app = router.finish_api(&mut api).layer(Extension(db));

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
