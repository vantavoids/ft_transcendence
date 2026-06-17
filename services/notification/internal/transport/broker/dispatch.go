package broker

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"log"
	"strconv"

	amqp "github.com/rabbitmq/amqp091-go"
	er "github.com/vantavoids/ft_transcendence/services/notification/internal/errors"
	notif "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
)

const (
	TypeMention       = "mention"
	TypeDM            = "dm"
	TypeFriendRequest = "friend_request"
	TypeGuildInvite   = "guild_invite"
	TypeGuildWelcome  = "guild_welcome"
	TypeIncomingCall  = "incoming_call"
)

// Dispatch processes a single AMQP delivery and routes it to the appropriate notification handler via svc.
func Dispatch(ctx context.Context, svc *notif.Service, d amqp.Delivery) error {
	switch d.RoutingKey {

	case "chat.message_sent":
		ev, err := parse[ChatMessageSentEvent](d)
		if err != nil {
			return err
		}
		for _, m := range ev.Mentions {
			uid, err := strconv.ParseInt(m, 10, 64)
			if err != nil {
				return err
			}
			// TODO: if Create fail at the third notification, the nack will redo the entire event
			// and then copy of the first two notification will be present in the db
			if err := svc.Create(ctx, notif.CreateInput{
				UserID:   uid,
				Type:     TypeMention,
				ActorID:  &ev.AuthorID,
				SourceID: &ev.MessageID,
				Payload: MentionPayload{
					ChannelID: ev.ChannelID,
					GuildID:   ev.GuildID,
					Preview:   ev.Content,
				},
			}); err != nil {
				return err
			}
		}
		return nil

	case "chat.dm_sent":
		ev, err := parse[ChatDmSentEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.RecipientID,
			Type:     TypeDM,
			ActorID:  &ev.SenderID,
			SourceID: &ev.MessageID,
			Payload: DmPayload{
				ConversationID: ev.ConversationID,
				Preview:        ev.Content,
			},
		})

	case "friend.request_sent":
		ev, err := parse[FriendRequestSentEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.AddresseeID,
			Type:     TypeFriendRequest,
			ActorID:  &ev.RequesterID,
			SourceID: &ev.FriendshipID,
			Payload:  FriendRequestPayload{},
		})

	case "guild.invite_created":
		ev, err := parse[GuildInviteCreatedEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.InvitedUserID,
			Type:     TypeGuildInvite,
			ActorID:  &ev.InvitedByUserID,
			SourceID: &ev.GuildID,
			Payload: GuildInvitePayload{
				GuildName: ev.GuildName,
			},
		})

	case "guild.member_joined":
		ev, err := parse[GuildMemberJoinedEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.UserID,
			Type:     TypeGuildWelcome,
			ActorID:  nil,
			SourceID: &ev.GuildID,
			Payload: GuildWelcomePayload{
				GuildName: ev.GuildName,
			},
		})

	case "call.incoming":
		ev, err := parse[CallIncomingEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.CalleeID,
			Type:     TypeIncomingCall,
			ActorID:  &ev.CallerID,
			SourceID: nil,
			Payload: IncomingCallPayload{
				CallType: ev.CallType,
			},
		})

	case "user.deleted":
		// TODO: clear all uid's notification

	default:
		log.Printf("unknown routing key: %s", d.RoutingKey)
		return nil
	}
	return nil
}

// Decode a delivery into the struct [T] and return an error if a field is wrong.
func decode[T any](d amqp.Delivery) (T, error) {
	var ev T
	dec := json.NewDecoder(bytes.NewReader(d.Body))
	dec.DisallowUnknownFields()
	if err := dec.Decode(&ev); err != nil {
		return ev, err
	}
	return ev, nil
}

type Validator interface {
	Validate() error
}

// Parse processes the decoding and return an error upon Validate().
func parse[T Validator](d amqp.Delivery) (T, error) {
	ev, err := decode[T](d)
	if err != nil {
		return ev, fmt.Errorf("parsing: %w: %s", er.ErrorPermanent, err)
	}
	if err := ev.Validate(); err != nil {
		return ev, fmt.Errorf("parsing: %w: %s", er.ErrorPermanent, err)
	}
	return ev, nil
}
