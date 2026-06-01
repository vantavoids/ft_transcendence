use axum::{
    http::{header::AUTHORIZATION, HeaderMap, StatusCode},
    Json,
};
use base64::{engine::general_purpose::URL_SAFE_NO_PAD, Engine as _};

use crate::dto::ErrorResponse;

pub fn caller_id_from_headers(
    headers: &HeaderMap,
) -> Result<i64, (StatusCode, Json<ErrorResponse>)> {
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

fn unauthorized() -> (StatusCode, Json<ErrorResponse>) {
    (
        StatusCode::UNAUTHORIZED,
        Json(ErrorResponse {
            error: "unauthorized".to_string(),
        }),
    )
}
