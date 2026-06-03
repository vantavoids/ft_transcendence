use std::sync::Arc;

use aide::openapi::OpenApi;
use axum::{
    extract::{
        rejection::{JsonRejection, QueryRejection},
        Json, Path, Query,
    },
    http::StatusCode,
    Extension,
};

use crate::{
    auth::caller_id_from_headers,
    dto::{ErrorResponse, UpdateUserRequest, UserProfile, UserSummary, UsersQuery},
    repository::{fetch_user_profile, fetch_user_summaries, update_user_profile},
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
    query: Result<Query<UsersQuery>, QueryRejection>,
) -> Result<Json<Vec<UserSummary>>, (StatusCode, Json<ErrorResponse>)> {
    let Query(query) = match query {
        Ok(query) => query,
        Err(_) => {
            return Err(bad_request(
                "ids query parameter is required and no other query parameters are allowed.",
            ));
        }
    };

    let Some(ids_value) = query.ids.as_deref() else {
        return Err(bad_request("ids query parameter is required."));
    };

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

pub async fn get_me(
    Extension(state): Extension<Arc<AppState>>,
    headers: axum::http::HeaderMap,
) -> Result<Json<UserProfile>, (StatusCode, Json<ErrorResponse>)> {
    let caller_id = match caller_id_from_headers(&headers) {
        Ok(id) => id,
        Err(response) => return Err(response),
    };

    let profile = match fetch_user_profile(&state.db, caller_id).await {
        Ok(Some(profile)) => profile,
        Ok(None) => return Err(not_found("user not found.")),
        Err(_) => return Err(internal_error()),
    };

    Ok(Json(profile))
}

pub async fn get_user(
    Extension(state): Extension<Arc<AppState>>,
    headers: axum::http::HeaderMap,
    Path(id): Path<i64>,
) -> Result<Json<UserProfile>, (StatusCode, Json<ErrorResponse>)> {
    if id <= 0 {
        return Err(bad_request("id must be a positive snowflake."));
    }

    let _caller_id = match caller_id_from_headers(&headers) {
        Ok(id) => id,
        Err(response) => return Err(response),
    };

    let profile = match fetch_user_profile(&state.db, id).await {
        Ok(Some(profile)) => profile,
        Ok(None) => return Err(not_found("user not found.")),
        Err(_) => return Err(internal_error()),
    };

    Ok(Json(profile))
}

pub async fn patch_user(
    Extension(state): Extension<Arc<AppState>>,
    headers: axum::http::HeaderMap,
    Path(id): Path<i64>,
    body: Result<Json<UpdateUserRequest>, JsonRejection>,
) -> Result<Json<UserProfile>, (StatusCode, Json<ErrorResponse>)> {
    if id <= 0 {
        return Err(bad_request("id must be a positive snowflake."));
    }

    let caller_id = match caller_id_from_headers(&headers) {
        Ok(id) => id,
        Err(response) => return Err(response),
    };

    if caller_id != id {
        return Err(forbidden("cannot update another user's profile."));
    }

    let Json(body) = match body {
        Ok(body) => body,
        Err(_) => return Err(bad_request("invalid request body.")),
    };

    if let Some(display_name) = body.display_name.as_ref() {
        if display_name.chars().count() > 64 {
            return Err(bad_request("display_name is too long."));
        }
    }

    let profile = match update_user_profile(
        &state.db,
        id,
        body.display_name.as_deref(),
        body.bio.as_deref(),
        body.status,
    )
    .await
    {
        Ok(Some(profile)) => profile,
        Ok(None) => return Err(not_found("user not found.")),
        Err(_) => return Err(internal_error()),
    };

    Ok(Json(profile))
}

fn bad_request(message: &str) -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::BAD_REQUEST,
        Json(ErrorResponse {
            error: message.to_string(),
        }),
    )
}

fn forbidden(message: &str) -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::FORBIDDEN,
        Json(ErrorResponse {
            error: message.to_string(),
        }),
    )
}

fn not_found(message: &str) -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::NOT_FOUND,
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
