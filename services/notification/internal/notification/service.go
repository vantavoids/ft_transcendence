package notif

import (
	"context"
	"encoding/json"

	db "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
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

func (s *Service) Create(ctx context.Context, in CreateInput) error {

	// TODO: We have to create a more specific return type for the Ack Nack in dispatch
	// - If the user service or database is blocked (try again) Nack(false, true)
	// - If the marshal failed (impossible to parse) Nack(false, false)

	// TODO: Should fail/open or fail/close when IsBlockedBy (user service down)
	// this means that the event has to retry until user service is up again, this can soft lock all events because we are running on only one worker 
	if in.ActorID != nil {
		blocked, err := s.clientUser.IsBlockedBy(ctx, in.UserID, *in.ActorID)
		if err != nil {
			return err
		}
		if blocked {
			return nil
		}
	}

	raw, err := json.Marshal(in.Payload)
	if err != nil {
		return err
	}

	id, err := s.snowflake.Generate()
	if err != nil {
		return err
	}

	// TODO: Push this notification into signalR when the hub is set
	_, err = s.db.CreateNotification(ctx, db.CreateNotificationParams{
		ID:       id,
		UserID:   in.UserID,
		Type:     db.NotificationType(in.Type),
		ActorID:  in.ActorID,
		SourceID: in.SourceID,
		Payload:  raw,
	})
	if err != nil {
		return err
	}

	return nil
}
