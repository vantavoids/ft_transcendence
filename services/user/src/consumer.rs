use std::time::Duration;

use futures_util::StreamExt;
use lapin::{
    options::{
        BasicAckOptions, BasicConsumeOptions, BasicNackOptions, ExchangeDeclareOptions,
        QueueBindOptions, QueueDeclareOptions,
    },
    types::FieldTable,
    Connection, ConnectionProperties, ExchangeKind,
};
use serde::Deserialize;
use sqlx::PgPool;

use crate::repository::create_default_profile;

const EVENT_EXCHANGE: &str = "Auth.Application.Events:UserRegisteredEvent";
const QUEUE_NAME: &str = "user-service.user-registered";

#[derive(Debug, Deserialize)]
struct UserRegisteredEvent {
    #[serde(alias = "userId")]
    user_id: i64,
    email: String,
}

pub fn spawn_user_registered_consumer(db: PgPool) {
    tokio::spawn(async move {
        loop {
            if let Err(err) = run_user_registered_consumer(&db).await {
                eprintln!("user.registered consumer stopped: {err}");
            }

            tokio::time::sleep(Duration::from_secs(5)).await;
        }
    });
}

async fn run_user_registered_consumer(
    db: &PgPool,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let connection = Connection::connect(
        &rabbitmq_uri(),
        ConnectionProperties::default()
            .with_executor(tokio_executor_trait::Tokio::current())
            .with_reactor(tokio_reactor_trait::Tokio),
    )
    .await?;

    let channel = connection.create_channel().await?;
    channel
        .exchange_declare(
            EVENT_EXCHANGE,
            ExchangeKind::Fanout,
            ExchangeDeclareOptions {
                passive: true,
                durable: false,
                auto_delete: false,
                internal: false,
                nowait: false,
            },
            FieldTable::default(),
        )
        .await?;

    let queue = channel
        .queue_declare(
            QUEUE_NAME,
            QueueDeclareOptions {
                passive: false,
                durable: true,
                exclusive: false,
                auto_delete: false,
                nowait: false,
            },
            FieldTable::default(),
        )
        .await?;

    channel
        .queue_bind(
            queue.name().as_str(),
            EVENT_EXCHANGE,
            "",
            QueueBindOptions::default(),
            FieldTable::default(),
        )
        .await?;

    let mut consumer = channel
        .basic_consume(
            queue.name().as_str(),
            "user-service.user-registered",
            BasicConsumeOptions::default(),
            FieldTable::default(),
        )
        .await?;

    while let Some(message) = consumer.next().await {
        let delivery = match message {
            Ok(delivery) => delivery,
            Err(err) => {
                eprintln!("user.registered consumer delivery error: {err}");
                continue;
            }
        };

        let payload = match decode_user_registered_event(&delivery.data) {
            Ok(payload) => payload,
            Err(err) => {
                eprintln!("user.registered consumer received invalid payload: {err}");
                delivery.ack(BasicAckOptions::default()).await?;
                continue;
            }
        };

        let username = default_username(&payload.email, payload.user_id);
        let result = create_default_profile(db, payload.user_id, &username).await;
        let result = match result {
            Ok(result) => Ok(result),
            Err(err) if is_unique_violation(&err) => {
                let fallback = fallback_username(&username, payload.user_id);
                create_default_profile(db, payload.user_id, &fallback).await
            }
            Err(err) => Err(err),
        };

        match result {
            Ok(_) => {
                delivery.ack(BasicAckOptions::default()).await?;
            }
            Err(err) => {
                eprintln!(
                    "user.registered consumer failed for user_id {}: {err}",
                    payload.user_id
                );
                delivery
                    .nack(BasicNackOptions {
                        multiple: false,
                        requeue: true,
                    })
                    .await?;
            }
        }
    }

    Ok(())
}

fn rabbitmq_uri() -> String {
    let host = std::env::var("RABBITMQ_HOST").unwrap_or_else(|_| "rabbitmq".to_string());
    let port = std::env::var("RABBITMQ_PORT").unwrap_or_else(|_| "5672".to_string());
    let username = std::env::var("RABBITMQ_USERNAME")
        .or_else(|_| std::env::var("RABBITMQ_USER"))
        .unwrap_or_else(|_| "guest".to_string());
    let password = std::env::var("RABBITMQ_PASSWORD")
        .or_else(|_| std::env::var("RABBITMQ_PASS"))
        .unwrap_or_else(|_| "guest".to_string());
    let vhost = std::env::var("RABBITMQ_VHOST").unwrap_or_else(|_| "/".to_string());

    format!(
        "amqp://{username}:{password}@{host}:{port}/{}",
        encode_vhost(&vhost)
    )
}

fn encode_vhost(vhost: &str) -> String {
    if vhost == "/" {
        "%2f".to_string()
    } else {
        vhost
            .chars()
            .flat_map(|ch| match ch {
                '%' => "%25".chars().collect::<Vec<_>>(),
                '/' => "%2f".chars().collect::<Vec<_>>(),
                ':' => "%3a".chars().collect::<Vec<_>>(),
                '@' => "%40".chars().collect::<Vec<_>>(),
                _ => vec![ch],
            })
            .collect()
    }
}

fn default_username(email: &str, user_id: i64) -> String {
    // Keep the email prefix shape, but normalize it to fit the username column.
    let local_part = email
        .split_once('@')
        .map(|(left, _)| left)
        .unwrap_or(email)
        .trim()
        .to_lowercase();

    let mut username = local_part
        .chars()
        .map(|ch| match ch {
            'a'..='z' | '0'..='9' | '_' => ch,
            _ => '_',
        })
        .collect::<String>();

    while username.contains("__") {
        username = username.replace("__", "_");
    }
    username = username.trim_matches('_').to_string();

    if username.is_empty() {
        username = format!("user-{user_id}");
    }

    username.chars().take(32).collect()
}

fn fallback_username(username: &str, user_id: i64) -> String {
    let suffix = format!("-{user_id}");
    let max_prefix_len = 32usize.saturating_sub(suffix.len());
    let mut candidate = username.chars().take(max_prefix_len).collect::<String>();
    candidate.push_str(&suffix);
    candidate
}

fn is_unique_violation(err: &sqlx::Error) -> bool {
    matches!(
        err,
        sqlx::Error::Database(db_err) if db_err.code().as_deref() == Some("23505")
    )
}

fn decode_user_registered_event(
    data: &[u8],
) -> Result<UserRegisteredEvent, serde_json::Error> {
    let value: serde_json::Value = serde_json::from_slice(data)?;

    if let Some(message) = value.get("message") {
        return serde_json::from_value(message.clone());
    }

    serde_json::from_value(value)
}

#[cfg(test)]
mod tests {
    use super::decode_user_registered_event;

    #[test]
    fn decodes_mass_transit_envelope_payload() {
        let payload = br#"
        {
            "messageId": "0c010000-d240-3f2d-2c64-08dec241d564",
            "sentTime": "2026-06-04T14:01:53.000Z",
            "messageType": ["urn:message:Auth.Application.Events:UserRegisteredEvent"],
            "message": {
                "user_id": 123,
                "email": "basic-user@example.com"
            }
        }
        "#;

        let event = decode_user_registered_event(payload).expect("expected envelope to decode");

        assert_eq!(event.user_id, 123);
        assert_eq!(event.email, "basic-user@example.com");
    }

    #[test]
    fn decodes_raw_payload() {
        let payload = br#"{ "user_id": 123, "email": "basic-user@example.com" }"#;

        let event = decode_user_registered_event(payload).expect("expected raw payload to decode");

        assert_eq!(event.user_id, 123);
        assert_eq!(event.email, "basic-user@example.com");
    }

    #[test]
    fn decodes_camel_case_payload_with_event_type() {
        let payload = br#"
        {
            "eventType": "user.registered",
            "userId": 123,
            "email": "basic-user@example.com"
        }
        "#;

        let event = decode_user_registered_event(payload).expect("expected camelCase payload to decode");

        assert_eq!(event.user_id, 123);
        assert_eq!(event.email, "basic-user@example.com");
    }
}
