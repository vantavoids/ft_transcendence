use sqlx::PgPool;

use crate::dto::{UserProfile, UserProfileRow, UserStatus, UserSummary, UserSummaryRow};

pub async fn fetch_user_summaries(
    db: &PgPool,
    caller_id: i64,
    ids: &[i64],
) -> Result<Vec<UserSummary>, sqlx::Error> {
    let rows = sqlx::query_as::<_, UserSummaryRow>(
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

    Ok(rows.into_iter().map(UserSummary::from).collect())
}

pub async fn fetch_user_profile(
    db: &PgPool,
    user_id: i64,
) -> Result<Option<UserProfile>, sqlx::Error> {
    let row = sqlx::query_as::<_, UserProfileRow>(
        r#"
        SELECT
            p.id,
            p.username,
            COALESCE(p.display_name, p.username) AS display_name,
            p.avatar_url,
            p.banner_url,
            p.status::text AS status,
            p.bio,
            p.last_seen_at
        FROM users_profile p
        WHERE p.id = $1
        "#,
    )
    .bind(user_id)
    .fetch_optional(db)
    .await?;

    Ok(row.map(UserProfile::from))
}

pub async fn update_user_profile(
    db: &PgPool,
    user_id: i64,
    display_name: Option<&str>,
    bio: Option<&str>,
    status: Option<UserStatus>,
) -> Result<Option<UserProfile>, sqlx::Error> {
    let row = sqlx::query_as::<_, UserProfileRow>(
        r#"
        UPDATE users_profile
        SET
            display_name = COALESCE($1, display_name),
            bio = COALESCE($2, bio),
            status = COALESCE($3::user_status, status),
            updated_at = NOW()
        WHERE id = $4
        RETURNING
            id,
            username,
            COALESCE(display_name, username) AS display_name,
            avatar_url,
            banner_url,
            status::text AS status,
            bio,
            last_seen_at
        "#,
    )
    .bind(display_name)
    .bind(bio)
    .bind(status.map(UserStatus::as_db_str))
    .bind(user_id)
    .fetch_optional(db)
    .await?;

    Ok(row.map(UserProfile::from))
}

pub async fn create_default_profile(
    db: &PgPool,
    user_id: i64,
    username: &str,
) -> Result<bool, sqlx::Error> {
    let result = sqlx::query(
        r#"
        INSERT INTO users_profile (id, username, status)
        VALUES ($1, $2, 'offline'::user_status)
        ON CONFLICT (id) DO NOTHING
        "#,
    )
    .bind(user_id)
    .bind(username)
    .execute(db)
    .await?;

    Ok(result.rows_affected() > 0)
}
