use std::sync::Arc;

use aide::{
    axum::{
        routing::{get, get_with},
        ApiRouter,
    },
    openapi::{Info, OpenApi},
};
use axum::{
    extract::Query,
    http::{header::AUTHORIZATION, HeaderMap, StatusCode},
    Extension, Json,
};
use base64::{engine::general_purpose::URL_SAFE_NO_PAD, Engine as _};
use schemars::JsonSchema;
use serde::Deserialize;
use sqlx::{postgres::PgPoolOptions, FromRow, PgPool};

#[derive(Debug, Clone)]
struct AppState {
    db: PgPool,
}

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
struct UserSummary {
    id: i64,
    username: String,
    display_name: String,
    avatar_url: Option<String>,
    status: String,
}

#[derive(Debug, Deserialize, JsonSchema)]
struct UsersQuery {
    ids: Option<String>,
    q: Option<String>,
}

#[derive(Debug, serde::Serialize, JsonSchema)]
struct ErrorResponse {
    error: String,
}

async fn hello() -> &'static str {
    "tu compiles hein"
}

async fn serve_openapi(Extension(api): Extension<Arc<OpenApi>>) -> Json<OpenApi> {
    Json((*api).clone())
}

async fn get_users(
    Extension(state): Extension<Arc<AppState>>,
    headers: HeaderMap,
    Query(query): Query<UsersQuery>,
) -> Result<Json<Vec<UserSummary>>, (StatusCode, Json<ErrorResponse>)> {
    let Some(ids_value) = query.ids.as_deref() else {
        return Err(bad_request(
            "ids query parameter is required and cannot be combined with q.",
        ));
    };

    if query.q.is_some() {
        return Err(bad_request(
            "ids query parameter is required and cannot be combined with q.",
        ));
    }

    let caller_id = match caller_id_from_headers(&headers) {
        Ok(id) => id,
        Err(response) => return Err(response),
    };

    let ids = match parse_ids(ids_value) {
        Ok(ids) => ids,
        Err(message) => return Err(bad_request(&message)),
    };

    let rows = match fetch_user_summaries(&state.db, caller_id, &ids).await {
        Ok(rows) => rows,
        Err(_) => return Err(internal_error()),
    };

    Ok(Json(rows))
}

fn bad_request(message: &str) -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::BAD_REQUEST,
        Json(ErrorResponse {
            error: message.to_string(),
        }),
    )
}

fn unauthorized() -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::UNAUTHORIZED,
        Json(ErrorResponse {
            error: "unauthorized".to_string(),
        }),
    )
}

fn internal_error() -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::INTERNAL_SERVER_ERROR,
        Json(ErrorResponse {
            error: "internal_server_error".to_string(),
        }),
    )
}

fn parse_ids(value: &str) -> Result<Vec<i64>, String> {
    let mut ids = Vec::new();

    for part in value.split(',') {
        let trimmed = part.trim();
        if trimmed.is_empty() {
            return Err("ids query parameter contains an empty identifier.".to_string());
        }

        let id = trimmed
            .parse::<i64>()
            .map_err(|_| format!("invalid snowflake ID: {trimmed}"))?;
        if id <= 0 {
            return Err(format!("invalid snowflake ID: {trimmed}"));
        }

        ids.push(id);
    }

    if ids.is_empty() {
        return Err("ids query parameter is required.".to_string());
    }
    if ids.len() > 100 {
        return Err("ids query parameter accepts at most 100 IDs.".to_string());
    }

    Ok(ids)
}

fn caller_id_from_headers(headers: &HeaderMap) -> Result<i64, (StatusCode, Json<ErrorResponse>)> {
    let auth = headers
        .get(AUTHORIZATION)
        .and_then(|value| value.to_str().ok())
        .ok_or_else(unauthorized)?;

    let token = auth
        .strip_prefix("Bearer ")
        .or_else(|| auth.strip_prefix("bearer "))
        .ok_or_else(unauthorized)?;

    let mut parts = token.split('.');
    let _header = parts.next().ok_or_else(unauthorized)?;
    let payload = parts.next().ok_or_else(unauthorized)?;
    let _signature = parts.next().ok_or_else(unauthorized)?;
    if parts.next().is_some() {
        return Err(unauthorized());
    }

    let payload = URL_SAFE_NO_PAD
        .decode(payload)
        .map_err(|_| unauthorized())?;
    let claims: serde_json::Value = serde_json::from_slice(&payload).map_err(|_| unauthorized())?;
    let sub = claims
        .get("sub")
        .and_then(|value| value.as_str())
        .ok_or_else(unauthorized)?;

    let id = sub.parse::<i64>().map_err(|_| unauthorized())?;
    if id <= 0 {
        return Err(unauthorized());
    }

    Ok(id)
}

async fn fetch_user_summaries(
    db: &PgPool,
    caller_id: i64,
    ids: &[i64],
) -> Result<Vec<UserSummary>, sqlx::Error> {
    let rows = sqlx::query_as::<_, UserSummary>(
        r#"
        WITH requested(id, ord) AS (
            SELECT *
            FROM unnest($1::bigint[]) WITH ORDINALITY
        )
        SELECT
            p.id,
            p.username,
            COALESCE(p.display_name, p.username) AS display_name,
            p.avatar_url,
            p.status::text AS status
        FROM requested r
        JOIN users_profile p ON p.id = r.id
        WHERE NOT EXISTS (
            SELECT 1
            FROM user_blocks b
            WHERE (b.blocker_id = $2 AND b.blocked_id = r.id)
               OR (b.blocker_id = r.id AND b.blocked_id = $2)
        )
        ORDER BY r.ord
        "#,
    )
    .bind(ids)
    .bind(caller_id)
    .fetch_all(db)
    .await?;

    Ok(rows)
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

    let mut api = OpenApi {
        info: Info {
            title: "User Service".into(),
            version: "1.0.0".into(),
            ..Default::default()
        },
        ..Default::default()
    };

    let mut router = ApiRouter::new()
        .api_route("/v1/hello-world", get(hello))
        .api_route("/v1/users", get_with(get_users, |t| t));

    if is_dev {
        router = router.route("/openapi/v1.json", axum::routing::get(serve_openapi));
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
