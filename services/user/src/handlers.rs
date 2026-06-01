use std::sync::Arc;

use aide::openapi::OpenApi;
use axum::{extract::Query, http::StatusCode, Extension, Json};

use crate::{
    auth::caller_id_from_headers,
    dto::{ErrorResponse, UserSummary, UsersQuery},
    repository::fetch_user_summaries,
    AppState,
};

pub async fn hello() -> &'static str {
    "tu compiles hein"
}

pub async fn serve_openapi(Extension(api): Extension<Arc<OpenApi>>) -> Json<OpenApi> {
    Json((*api).clone())
}

pub async fn get_users(
    Extension(state): Extension<Arc<AppState>>,
    headers: axum::http::HeaderMap,
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
