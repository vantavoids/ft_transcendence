package core

import (
	"context"
	"encoding/json"
	"fmt"
	"log"

	db "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	er "github.com/vantavoids/ft_transcendence/services/notification/internal/errors"
	sflk "github.com/vantavoids/ft_transcendence/services/notification/internal/snowflake"
)

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

type Service struct {
	db         *db.Queries
	snowflake  *sflk.SnowflakeGenerator
	clientUser RelationshipChecker
}

func NewService(db *db.Queries, sflk *sflk.SnowflakeGenerator, userClient RelationshipChecker) (*Service, error) {
	return &Service{db: db, snowflake: sflk, clientUser: userClient}, nil
}

func (n *Service) Create(ctx context.Context, in CreateInput) error {
	// TODO: Should fail/open or fail/close when IsBlockedBy (user service down)
	// this means that the event has to retry until user service is up again, this can soft lock all events because we are running on only one worker
	if in.ActorID != nil {
		blocked, err := n.clientUser.IsBlockedBy(ctx, in.UserID, *in.ActorID)
		if err != nil {
			return fmt.Errorf("user client: %w: %s", er.ErrorTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", in.UserID, *in.ActorID)
			return nil
		}
	}

	raw, err := json.Marshal(in.Payload)
	if err != nil {
		return fmt.Errorf("masharl payload: %w: %s", er.ErrorPermanent, err)
	}

	id, err := n.snowflake.Generate()
	if err != nil {
		return fmt.Errorf("snowflake generate: %w: %s", er.ErrorTemporary, err)
	}

	// TODO: Push this notification into signalR when the hub is set
	// TODO: maybe an ON CONFLICT DO NOTHING if rabbitmq send two times the same exact publish
	_, err = n.db.CreateNotification(ctx, db.CreateNotificationParams{
		ID:       id,
		UserID:   in.UserID,
		Type:     db.NotificationType(in.Type),
		ActorID:  in.ActorID,
		SourceID: in.SourceID,
		Payload:  raw,
	})
	if err != nil {
		return fmt.Errorf("insert notification: %w: %s", er.ErrorTemporary, err)
	}
	log.Printf("notification created: id=%d type=%s user=%d", id, in.Type, in.UserID)
	return nil
}
