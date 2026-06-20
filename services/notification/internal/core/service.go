package core

import (
	"context"
	"encoding/json"
	"fmt"
	"log"

	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
	snowflake "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/snowflake"
)

type Service struct {
	db         *database.Queries
	snowflake  *snowflake.Generator
	clientUser RelationshipChecker
}

func NewService(db *database.Queries, sflk *snowflake.Generator, userClient RelationshipChecker) (*Service, error) {
	return &Service{db: db, snowflake: sflk, clientUser: userClient}, nil
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
			return fmt.Errorf("user client: %w: %s", failure.ErrorTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", in.UserID, *in.ActorID)
			return nil
		}
	}

	raw, err := json.Marshal(in.Payload)
	if err != nil {
		return fmt.Errorf("masharl payload: %w: %s", failure.ErrorPermanent, err)
	}

	id, err := n.snowflake.Generate()
	if err != nil {
		return fmt.Errorf("snowflake generate: %w: %s", failure.ErrorTemporary, err)
	}

	// TODO: Push this notification into signalR when the hub is set
	// TODO: maybe an ON CONFLICT DO NOTHING if rabbitmq send two times the same exact publish
	_, err = s.db.CreateNotification(ctx, database.CreateNotificationParams{
		ID:       id,
		UserID:   in.UserID,
		Type:     database.NotificationType(in.Type),
		ActorID:  in.ActorID,
		SourceID: in.SourceID,
		Payload:  raw,
	})
	if err != nil {
		return fmt.Errorf("insert notification: %w: %s", failure.ErrorTemporary, err)
	}
	log.Printf("notification created: id=%d type=%s user=%d", id, in.Type, in.UserID)
	return nil
}

// func (s *Service) MarkRead(ctx context.Context) error {

// }
