use schemars::JsonSchema;
use serde::Deserialize;
use sqlx::FromRow;

#[derive(Debug, FromRow, serde::Serialize, JsonSchema)]
pub struct UserSummary {
    pub id: i64,
    pub username: String,
    pub display_name: String,
    pub avatar_url: Option<String>,
    pub status: String,
}

#[derive(Debug, Deserialize, JsonSchema)]
#[serde(deny_unknown_fields)]
pub struct UsersQuery {
    pub ids: Option<String>,
}

#[derive(Debug, serde::Serialize, JsonSchema)]
pub struct ErrorResponse {
    pub error: String,
}
