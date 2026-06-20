package api

import (
	"encoding/json"
	"strconv"
	"time"

	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
)

type NotificationTypeDTO string

const (
	NotificationTypeMention       NotificationTypeDTO = "mention"
	NotificationTypeDm            NotificationTypeDTO = "dm"
	NotificationTypeFriendRequest NotificationTypeDTO = "friend_request"
	NotificationTypeGuildInvite   NotificationTypeDTO = "guild_invite"
	NotificationTypeGuildWelcome  NotificationTypeDTO = "guild_welcome"
	NotificationTypeIncomingCall  NotificationTypeDTO = "incoming_call"
)

type NotificationDTO struct {
	ID          string              `json:"id"`
	UserID      string              `json:"user_id"`
	Type        NotificationTypeDTO `json:"type"`
	ActorID     *string             `json:"actor_id"`
	SourceID    *string             `json:"source_id"`
	Payload     json.RawMessage     `json:"payload"`
	Read        bool                `json:"read"`
	DismissedAt *time.Time          `json:"dismissed_at"`
	CreatedAt   time.Time           `json:"created_at"`
}

func ToDTO(notif database.Notification) NotificationDTO {
	id := strconv.FormatInt(notif.ID, 10)
	userID := strconv.FormatInt(notif.UserID, 10)

	var actorID *string
	if notif.ActorID != nil {
		a := strconv.FormatInt(*notif.ActorID, 10)
		actorID = &a
	}

	var sourceID *string
	if notif.SourceID != nil {
		s := strconv.FormatInt(*notif.SourceID, 10)
		sourceID = &s
	}

	var dismissedAt *time.Time
	if notif.DismissedAt.Valid {
		dismissedAt = &notif.DismissedAt.Time
	}

	return NotificationDTO{
		ID:          id,
		UserID:      userID,
		Type:        NotificationTypeDTO(notif.Type),
		ActorID:     actorID,
		SourceID:    sourceID,
		Payload:     notif.Payload,
		Read:        notif.ReadAt.Valid,
		DismissedAt: dismissedAt,
		CreatedAt:   notif.CreatedAt.Time,
	}
}
