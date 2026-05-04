use chrono::{DateTime, Utc};
use schemars::JsonSchema;
use sqlx::FromRow;

#[derive(Debug, Clone, serde::Serialize, JsonSchema, sqlx::Type)]
#[serde(rename_all = "snake_case")]
#[sqlx(type_name = "user_status", rename_all = "snake_case")]
pub enum UserStatus {
    Online,
    Idle,
    Dnd,
    Offline,
}

#[derive(Debug, Clone, serde::Serialize, JsonSchema, sqlx::Type)]
#[serde(rename_all = "snake_case")]
#[sqlx(type_name = "friendship_status", rename_all = "snake_case")]
pub enum FriendshipStatus {
    Pending,
    Accepted,
    Blocked,
}

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
pub struct UserProfile {
    pub id: i64,
    pub username: String,
    pub display_name: Option<String>,
    pub avatar_url: Option<String>,
    pub banner_url: Option<String>,
    pub status: UserStatus,
    pub last_seen_at: Option<DateTime<Utc>>,
    pub bio: Option<String>,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
}

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
pub struct Friendship {
    pub id: i64,
    pub requester_id: i64,
    pub addressee_id: i64,
    pub status: FriendshipStatus,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
}

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
pub struct UserBlock {
    pub blocker_id: i64,
    pub blocked_id: i64,
    pub created_at: DateTime<Utc>,
}
