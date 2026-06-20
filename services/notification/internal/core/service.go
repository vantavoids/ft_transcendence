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

type Service struct {
	queries    *database.Queries
	snowflake  *snowflake.Generator
	clientUser RelationshipChecker
}

func NewService(queries *database.Queries, sflk *snowflake.Generator, userClient RelationshipChecker) (*Service, error) {
	return &Service{queries: queries, snowflake: sflk, clientUser: userClient}, nil
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

func (s *Service) Create(ctx context.Context, in CreateInput) error {

	// TODO: Should fail/open or fail/close when IsBlockedBy (user service down)
	// this means that the event has to retry until user service is up again, this can soft lock all events because we are running on only one worker
	if in.ActorID != nil {
		blocked, err := n.userTunnel.IsBlockedBy(ctx, in.UserID, *in.ActorID)
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
		return fmt.Errorf("masharl payload: %w: %s", failure.FailPermanent, err)
	}

	id, err := n.snowflake.Generate()
	if err != nil {
		return fmt.Errorf("snowflake generate: %w: %s", failure.FailTemporary, err)
	}

	// TODO: Push this notification into signalR when the hub is set
	// TODO: maybe an ON CONFLICT DO NOTHING if rabbitmq send two times the same exact publish
	_, err = s.queries.CreateNotification(ctx, database.CreateNotificationParams{
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
	return nil
}

type ListInput struct {
	UserID           int64
	Read             *bool
	IncludeDismissed *bool
	Before           *int64
	RowLimit         int32
}

func (s *Service) List(ctx context.Context, in ListInput) {

}

func (s *Service) MarkRead(ctx context.Context, userID int64, id int64) error {

	notif, err := s.queries.GetNotificationByID(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return failure.ErrNotFound
	}
	if err != nil {
		return err
	}

	if notif.UserID != userID {
		return failure.ErrForbidden
	}

	rows, err := s.queries.MarkNotificationRead(ctx, database.MarkNotificationReadParams{
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

func (s *Service) MarkReadAll(ctx context.Context, userID int64) (int64, error) {

	rows, err := s.queries.MarkAllNotificationsRead(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (s *Service) UnreadCount(ctx context.Context, userID int64) (int64, error) {

	rows, err := s.queries.CountUnreadNotifications(ctx, userID)
	if err != nil {
		return 0, err
	}

	return rows, nil
}

func (s *Service) Dismiss(ctx context.Context, userID int64, id int64) error {

	notif, err := s.queries.GetNotificationByID(ctx, id)
	if errors.Is(err, pgx.ErrNoRows) {
		return failure.ErrNotFound
	}
	if err != nil {
		return err
	}

	if notif.UserID != userID {
		return failure.ErrForbidden
	}

	rows, err := s.queries.DismissNotification(ctx, database.DismissNotificationParams{
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
