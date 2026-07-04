package core

import (
	"encoding/json"
	"strconv"
	"time"

	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
)

type NotificationType string

const (
	NotificationTypeMention       NotificationType = "mention"
	NotificationTypeDm            NotificationType = "dm"
	NotificationTypeFriendRequest NotificationType = "friend_request"
	NotificationTypeGuildInvite   NotificationType = "guild_invite"
	NotificationTypeGuildWelcome  NotificationType = "guild_welcome"
	NotificationTypeIncomingCall  NotificationType = "incoming_call"
)

type NotificationREST struct {
	ID          string           `json:"id"`
	UserID      string           `json:"user_id"`
	Type        NotificationType `json:"type"`
	ActorID     *string          `json:"actor_id"`
	SourceID    *string          `json:"source_id"`
	Payload     json.RawMessage  `json:"payload"`
	Read        bool             `json:"read"`
	DismissedAt *time.Time       `json:"dismissed_at"`
	CreatedAt   time.Time        `json:"created_at"`
}

func ToREST(notif database.Notification) NotificationREST {
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

	return NotificationREST{
		ID:          id,
		UserID:      userID,
		Type:        NotificationType(notif.Type),
		ActorID:     actorID,
		SourceID:    sourceID,
		Payload:     notif.Payload,
		Read:        notif.ReadAt.Valid,
		DismissedAt: dismissedAt,
		CreatedAt:   notif.CreatedAt.Time,
	}
}

type NotificationSSE struct {
	ID        string           `json:"id"`
	Type      NotificationType `json:"type"`
	ActorID   *string          `json:"actor_id"`
	SourceID  *string          `json:"source_id"`
	Payload   json.RawMessage  `json:"payload"`
	Read      bool             `json:"read"`
	CreatedAt time.Time        `json:"created_at"`
}

func ToSSE(notif database.Notification) NotificationSSE {
	id := strconv.FormatInt(notif.ID, 10)

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

	return NotificationSSE{
		ID:        id,
		Type:      NotificationType(notif.Type),
		ActorID:   actorID,
		SourceID:  sourceID,
		Payload:   notif.Payload,
		Read:      notif.ReadAt.Valid,
		CreatedAt: notif.CreatedAt.Time,
	}
}

type PreferenceScopeType string

const (
	ScopeGuild   PreferenceScopeType = "guild"
	ScopeChannel PreferenceScopeType = "channel"
)

type PreferenceDTO struct {
	ScopeType  PreferenceScopeType `json:"scope_type"`
	ScopeID    string              `json:"scope_id"`
	Muted      bool                `json:"muted"`
	MutedUntil *time.Time          `json:"muted_until,omitempty"`
}

func ToPreferenceDTO(pref database.NotificationPreference) PreferenceDTO {
	dto := PreferenceDTO{
		ScopeType: PreferenceScopeType(pref.ScopeType),
		ScopeID:   strconv.FormatInt(pref.ScopeID, 10),
		Muted:     pref.Muted,
	}
	if pref.MutedUntil.Valid {
		dto.MutedUntil = &pref.MutedUntil.Time
	}
	return dto
}
