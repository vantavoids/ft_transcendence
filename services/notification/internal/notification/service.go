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

type Service struct {
	db   *db.Queries
	sflk *sflk.SnowflakeGenerator
}

func NewService(db *db.Queries, sflk *sflk.SnowflakeGenerator) (*Service, error) {
	return &Service{db: db, sflk: sflk}, nil
}

func (s *Service) Create(ctx context.Context, in CreateInput) error {

	// TODO: check if the Actor is blocked by the User, in that case just return 

	id, err := s.sflk.Generate()
	if err != nil {
		return err
	}

	raw, err := json.Marshal(in.Payload)
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
