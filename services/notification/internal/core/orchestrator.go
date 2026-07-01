package core

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgtype"
	"github.com/jackc/pgx/v5/pgxpool"
	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
	snowflake "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/snowflake"
)

type Orchestrator struct {
	db      *pgxpool.Pool
	queries *database.Queries
	hub     *Hub

	snowflake  *snowflake.Generator
	userTunnel RelationshipChecker
}

func NewOrchestrator(db *pgxpool.Pool, hub *Hub, queries *database.Queries, sflk *snowflake.Generator, userClient RelationshipChecker) (*Orchestrator, error) {
	return &Orchestrator{db: db, queries: queries, hub: hub, snowflake: sflk, userTunnel: userClient}, nil
}

type CreateInput struct {
	UserID   int64
	Type     string
	ActorID  *int64
	SourceID *int64
	Payload  any
}

type RelationshipChecker interface {
	IsBlockedBy(ctx context.Context, targetID int64, senderID int64) (bool, error)
}

func (o *Orchestrator) CreateNotif(ctx context.Context, in CreateInput) error {

	// TODO: Should fail/open or fail/close when IsBlockedBy (user service down)
	// this means that the event has to retry until user service is up again, this can soft lock all events because we are running on only one worker
	if in.ActorID != nil {
		blocked, err := o.userTunnel.IsBlockedBy(ctx, in.UserID, *in.ActorID)
		if err != nil {
			return fmt.Errorf("user client: %w: %s", failure.FailTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", in.UserID, *in.ActorID)
			return nil
		}
	}

	raw, err := json.Marshal(in.Payload)
	if err != nil {
		return fmt.Errorf("marshal payload: %w: %s", failure.FailPermanent, err)
	}

	id, err := o.snowflake.Generate()
	if err != nil {
		return fmt.Errorf("snowflake generate: %w: %s", failure.FailTemporary, err)
	}

	// TODO: maybe an ON CONFLICT DO NOTHING if rabbitmq send two times the same exact publish (nonce problem)
	n, err := o.queries.CreateNotification(ctx, database.CreateNotificationParams{
		ID:       id,
		UserID:   in.UserID,
		Type:     database.NotificationType(in.Type),
		ActorID:  in.ActorID,
		SourceID: in.SourceID,
		Payload:  raw,
	})
	if err != nil {
		return fmt.Errorf("insert notification: %w: %s", failure.FailTemporary, err)
	}
	log.Printf("notification created: id=%d type=%s user=%d", id, in.Type, in.UserID)

	o.hub.Push(in.UserID, ToSSE(n))
	return nil
}

type ListInput struct {
	Read             *bool
	IncludeDismissed *bool
	Before           *int64
	RowLimit         int32
}

func (o *Orchestrator) ListNotifs(ctx context.Context, userID int64, in ListInput) ([]database.Notification, error) {
	notifs, err := o.queries.GetNotifications(ctx, database.GetNotificationsParams{
		UserID:           userID,
		Read:             in.Read,
		IncludeDismissed: in.IncludeDismissed,
		Before:           in.Before,
		RowLimit:         in.RowLimit,
	})
	if err != nil {
		return nil, err
	}

	return notifs, nil
}

func (o *Orchestrator) MarkReadNotif(ctx context.Context, userID int64, id int64) error {

	notif, err := o.queries.GetNotificationByID(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return failure.ErrNotFound
	}
	if err != nil {
		return err
	}

	if notif.UserID != userID {
		return failure.ErrForbidden
	}

	rows, err := o.queries.MarkNotificationRead(ctx, database.MarkNotificationReadParams{
		ID:     id,
		UserID: userID,
	})
	if err != nil {
		return err
	}

	if rows == 0 {
		return failure.ErrNotFound
	}

	return nil
}

func (o *Orchestrator) MarkReadAllNotifs(ctx context.Context, userID int64) (int64, error) {

	rows, err := o.queries.MarkAllNotificationsRead(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (o *Orchestrator) UnreadCountNotifs(ctx context.Context, userID int64) (int64, error) {

	rows, err := o.queries.CountUnreadNotifications(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (o *Orchestrator) DismissNotif(ctx context.Context, userID int64, id int64) error {

	notif, err := o.queries.GetNotificationByID(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return failure.ErrNotFound
	}
	if err != nil {
		return err
	}

	if notif.UserID != userID {
		return failure.ErrForbidden
	}

	rows, err := o.queries.DismissNotification(ctx, database.DismissNotificationParams{
		ID:     id,
		UserID: userID,
	})
	if err != nil {
		return err
	}

	if rows == 0 {
		return failure.ErrNotFound
	}

	return nil
}

func (o *Orchestrator) DeleteOlderNotifs(ctx context.Context) error {

	if err := o.queries.DeleteNotificationsOlderThan7Days(ctx); err != nil {
		return err
	}

	return nil
}

type UpsertInput struct {
	UserID     int64
	ScopeType  string
	ScopeID    int64
	Muted      bool
	MutedUntil time.Time
}

func (o *Orchestrator) UpsertPrefs(ctx context.Context, u UpsertInput) (database.NotificationPreference, error) {

	p, err := o.queries.UpsertNotificationPreference(ctx, database.UpsertNotificationPreferenceParams{
		UserID:     u.UserID,
		ScopeType:  u.ScopeType,
		ScopeID:    u.ScopeID,
		Muted:      u.Muted,
		MutedUntil: pgtype.Timestamptz{Time: u.MutedUntil, Valid: !u.MutedUntil.IsZero()},
	})
	if err != nil {
		return p, err
	}

	return p, nil
}

func (o *Orchestrator) ListPrefs(ctx context.Context, userID int64) ([]database.NotificationPreference, error) {

	p, err := o.queries.ListNotificationPreferences(ctx, userID)
	if err != nil {
		return p, err
	}

	return p, nil
}

func (o *Orchestrator) RemovePrefs(ctx context.Context, userID int64, scopeType string, scopeID int64) (int64, error) {

	rows, err := o.queries.RemoveNotificationPreference(ctx, database.RemoveNotificationPreferenceParams{
		UserID:    userID,
		ScopeType: scopeType,
		ScopeID:   scopeID,
	})
	if err != nil {
		return rows, err
	}

	return rows, nil
}

func (o *Orchestrator) IsMuted(ctx context.Context, userID int64, scopeType string, scopeID int64) (bool, error) {

	ok, err := o.queries.IsNotificationPreferenceMuted(ctx, database.IsNotificationPreferenceMutedParams{
		UserID:    userID,
		ScopeType: scopeType,
		ScopeID:   scopeID,
	})
	if err != nil {
		return ok, err
	}

	return ok, nil
}

func (o *Orchestrator) DeleteUserNotifs(ctx context.Context, userID int64) error {
	tx, err := o.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	qtx := o.queries.WithTx(tx)

	if err := qtx.DeleteUserNotifications(ctx, userID); err != nil {
		return err
	}

	if err := qtx.DeleteUserPreferences(ctx, userID); err != nil {
		return err
	}

	return nil
}
