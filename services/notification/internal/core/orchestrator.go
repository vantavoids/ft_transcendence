package core

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log"

	"github.com/jackc/pgx/v5"
	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
	snowflake "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/snowflake"
)

type Orchestrator struct {
	queries *database.Queries
	hub     *Hub

	snowflake  *snowflake.Generator
	userTunnel RelationshipChecker
}

func NewOrchestrator(hub *Hub, queries *database.Queries, sflk *snowflake.Generator, userClient RelationshipChecker) (*Orchestrator, error) {
	return &Orchestrator{queries: queries, hub: hub, snowflake: sflk, userTunnel: userClient}, nil
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

func (o *Orchestrator) Create(ctx context.Context, in CreateInput) error {

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

	o.hub.Push(id, ToSSE(n))
	return nil
}

type ListInput struct {
	Read             *bool
	IncludeDismissed *bool
	Before           *int64
	RowLimit         int32
}

func (o *Orchestrator) List(ctx context.Context, userID int64, in ListInput) ([]database.Notification, error) {
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

func (o *Orchestrator) MarkRead(ctx context.Context, userID int64, id int64) error {

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

func (o *Orchestrator) MarkReadAll(ctx context.Context, userID int64) (int64, error) {

	rows, err := o.queries.MarkAllNotificationsRead(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (o *Orchestrator) UnreadCount(ctx context.Context, userID int64) (int64, error) {

	rows, err := o.queries.CountUnreadNotifications(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (o *Orchestrator) Dismiss(ctx context.Context, userID int64, id int64) error {

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

// TODO: dont forget to add preferences in the delete section
// TODO: dont forget to add a rollback after adding the preference
func (o *Orchestrator) DeleteUser(ctx context.Context, userID int64) error {

	if err := o.queries.DeleteUserNotifications(ctx, userID); err != nil {
		return err
	}

	return nil
}

func (o *Orchestrator) DeleteOlder(ctx context.Context) error {

	if err := o.queries.DeleteNotificationsOlderThan7Days(ctx); err != nil {
		return err
	}

	return nil
}
