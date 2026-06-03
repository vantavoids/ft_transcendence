use sqlx::PgPool;

use crate::dto::UserSummary;

pub async fn fetch_user_summaries(
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
