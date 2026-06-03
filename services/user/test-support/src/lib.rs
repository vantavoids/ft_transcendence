use base64::{engine::general_purpose::URL_SAFE_NO_PAD, Engine as _};
use serde::Serialize;

pub fn bearer_token(sub: i64) -> String {
    let header = r#"{"alg":"none","typ":"JWT"}"#;
    let payload = serde_json::json!({ "sub": sub.to_string() });

    format!(
        "{}.{}.{}",
        URL_SAFE_NO_PAD.encode(header),
        URL_SAFE_NO_PAD.encode(payload.to_string()),
        "signature"
    )
}

pub fn bearer_authorization(sub: i64) -> String {
    format!("Bearer {}", bearer_token(sub))
}

pub fn json<T: Serialize>(value: &T) -> String {
    serde_json::to_string(value).expect("test payload serializes to json")
}
