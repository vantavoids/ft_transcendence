package amqp

import (
	"context"
	"encoding/json"
	"log"
	"strconv"

	amqp "github.com/rabbitmq/amqp091-go"
	notif "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
)

// Dispatch processes a single AMQP delivery and routes it to the appropriate notification handler via svc.
func Dispatch(ctx context.Context, svc *notif.Service, d amqp.Delivery) error {
	switch d.RoutingKey {

	case "chat.message_sent":
		ev, err := decode[ChatMessageSentEvent](d)
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
				Type:     "mention",
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
		ev, err := decode[ChatDmSentEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.RecipientID,
			Type:     "dm",
			ActorID:  &ev.SenderID,
			SourceID: &ev.MessageID,
			Payload: DmPayload{
				ConversationID: ev.ConversationID,
				Preview:        ev.Content,
			},
		})

	case "friend.request_sent":
		ev, err := decode[FriendRequestSentEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.AddresseeID,
			Type:     "friend_request",
			ActorID:  &ev.RequesterID,
			SourceID: &ev.FriendshipID,
			Payload:  FriendRequestPayload{},
		})

	case "guild.invite_created":
		ev, err := decode[GuildInviteCreatedEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.InvitedUserID,
			Type:     "guild_invite",
			ActorID:  &ev.InvitedByUserID,
			SourceID: &ev.GuildID,
			Payload: GuildInvitePayload{
				GuildName: ev.GuildName,
			},
		})

	case "guild.member_joined":
		ev, err := decode[GuildMemberJoinedEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.UserID,
			Type:     "guild_welcome",
			ActorID:  nil,
			SourceID: &ev.GuildID,
			Payload: GuildWelcomePayload{
				GuildName: ev.GuildName,
			},
		})

	case "call.incoming":
		ev, err := decode[CallIncomingEvent](d)
		if err != nil {
			return err
		}
		return svc.Create(ctx, notif.CreateInput{
			UserID:   ev.CalleeID,
			Type:     "incoming_call",
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

func decode[T any](d amqp.Delivery) (T, error) {
	var ev T
	if err := json.Unmarshal(d.Body, &ev); err != nil {
		return ev, err
	}
	return ev, nil
}
