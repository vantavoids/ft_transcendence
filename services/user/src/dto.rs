use chrono::{DateTime, Utc};
use schemars::JsonSchema;
use serde::{Deserialize, Serialize};
use sqlx::FromRow;

#[derive(Debug, FromRow)]
pub struct UserSummaryRow {
    pub id: i64,
    pub username: String,
    pub display_name: String,
    pub avatar_url: Option<String>,
    pub status: String,
}

#[derive(Debug, serde::Serialize, JsonSchema)]
pub struct UserSummary {
    pub id: String,
    pub username: String,
    pub display_name: String,
    pub avatar_url: Option<String>,
    pub status: String,
}

impl From<UserSummaryRow> for UserSummary {
    fn from(row: UserSummaryRow) -> Self {
        Self {
            id: row.id.to_string(),
            username: row.username,
            display_name: row.display_name,
            avatar_url: row.avatar_url,
            status: row.status,
        }
    }
}

#[derive(Debug, FromRow)]
pub struct UserProfileRow {
    pub id: i64,
    pub username: String,
    pub display_name: String,
    pub avatar_url: Option<String>,
    pub banner_url: Option<String>,
    pub status: String,
    pub bio: Option<String>,
    pub last_seen_at: Option<DateTime<Utc>>,
}

#[derive(Debug, serde::Serialize, JsonSchema)]
pub struct UserProfile {
    pub id: String,
    pub username: String,
    pub display_name: String,
    pub avatar_url: Option<String>,
    pub banner_url: Option<String>,
    pub status: String,
    pub bio: Option<String>,
    pub last_seen_at: Option<DateTime<Utc>>,
}

impl From<UserProfileRow> for UserProfile {
    fn from(row: UserProfileRow) -> Self {
        Self {
            id: row.id.to_string(),
            username: row.username,
            display_name: row.display_name,
            avatar_url: row.avatar_url,
            banner_url: row.banner_url,
            status: row.status,
            bio: row.bio,
            last_seen_at: row.last_seen_at,
        }
    }
}

#[derive(Debug, Deserialize, JsonSchema, Clone, Copy)]
#[serde(rename_all = "lowercase")]
pub enum UserStatus {
    Online,
    Idle,
    Dnd,
    Offline,
}

impl UserStatus {
    pub const fn as_db_str(self) -> &'static str {
        match self {
            Self::Online => "online",
            Self::Idle => "idle",
            Self::Dnd => "dnd",
            Self::Offline => "offline",
        }
    }
}

#[derive(Debug, Deserialize, JsonSchema)]
#[serde(deny_unknown_fields)]
pub struct UpdateUserRequest {
    pub display_name: Option<String>,
    pub bio: Option<String>,
    pub status: Option<UserStatus>,
}

#[derive(Debug, Deserialize, JsonSchema)]
#[serde(deny_unknown_fields)]
pub struct UsersQuery {
    pub ids: Option<String>,
}

#[derive(Debug, Serialize, JsonSchema)]
pub struct ErrorResponse {
    pub error: String,
}

#[cfg(test)]
mod tests {
    use super::UpdateUserRequest;

    #[test]
    fn patch_request_rejects_unknown_avatar_and_banner_fields() {
        for body in [
            r#"{"avatar_url":"https://example.com/avatar.png"}"#,
            r#"{"banner_url":"https://example.com/banner.png"}"#,
        ] {
            assert!(serde_json::from_str::<UpdateUserRequest>(body).is_err());
        }
    }
}
