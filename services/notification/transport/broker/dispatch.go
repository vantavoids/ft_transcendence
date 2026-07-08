package broker

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"log"
	"strconv"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
	email "github.com/vantavoids/ft_transcendence/services/notification/transport/email"
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
func dispatch(ctx context.Context, orch *core.Orchestrator, d amqp.Delivery) error {
	switch d.RoutingKey {

	case "chat.message_sent":
		ev, err := parse[ChatMessageSentEvent](d)
		if err != nil {
			return err
		}
		for _, m := range ev.Mentions {
			uid, err := strconv.ParseInt(m, 10, 64)
			if err != nil {
				return failure.FailPermanent
			}

			// TODO: Should fail/open or fail/close when IsBlockedBy (user service down)
			// this means that the event has to retry until user service is up again, this can soft lock all events because we are running on only one worker
			blocked, err := orch.UserTunnel.IsBlockedBy(ctx, uid, ev.AuthorID)
			if err != nil {
				return fmt.Errorf("user client: %w: %s", failure.FailTemporary, err)
			}
			if blocked {
				log.Printf("notification canceled: %d is blocked by %d", uid, ev.AuthorID)
				return nil
			}

			gOk, err := orch.IsMuted(ctx, uid, "guild", ev.GuildID)
			if err != nil {
				return failure.FailPermanent
			}

			cOk, err := orch.IsMuted(ctx, uid, "channel", ev.ChannelID)
			if err != nil {
				return failure.FailPermanent
			}

			if gOk == true || cOk == true {
				return nil
			}

			if err := orch.CreateNotif(ctx, core.CreateInput{
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

		blocked, err := orch.UserTunnel.IsBlockedBy(ctx, ev.RecipientID, ev.SenderID)
		if err != nil {
			return fmt.Errorf("user client: %w: %s", failure.FailTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", ev.RecipientID, ev.SenderID)
			return nil
		}

		return orch.CreateNotif(ctx, core.CreateInput{
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

		blocked, err := orch.UserTunnel.IsBlockedBy(ctx, ev.AddresseeID, ev.RequesterID)
		if err != nil {
			return fmt.Errorf("user client: %w: %s", failure.FailTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", ev.AddresseeID, ev.RequesterID)
			return nil
		}

		return orch.CreateNotif(ctx, core.CreateInput{
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

		// TODO: does guild invite is sent throught dm ?

		return orch.CreateNotif(ctx, core.CreateInput{
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

		gOk, err := orch.IsMuted(ctx, ev.UserID, "guild", ev.GuildID)
		if err != nil {
			return failure.FailPermanent
		}

		if gOk == true {
			return nil
		}

		return orch.CreateNotif(ctx, core.CreateInput{
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

		blocked, err := orch.UserTunnel.IsBlockedBy(ctx, ev.CalleeID, ev.CallerID)
		if err != nil {
			return fmt.Errorf("user client: %w: %s", failure.FailTemporary, err)
		}
		if blocked {
			log.Printf("notification canceled: %d is blocked by %d", ev.CalleeID, ev.CallerID)
			return nil
		}

		return orch.CreateNotif(ctx, core.CreateInput{
			UserID:   ev.CalleeID,
			Type:     TypeIncomingCall,
			ActorID:  &ev.CallerID,
			SourceID: nil,
			Payload: IncomingCallPayload{
				CallType: ev.CallType,
			},
		})

	case "user.deleted":
		ev, err := parse[UserDeletedEvent](d)
		if err != nil {
			return err
		}

		if err := orch.DeleteUserNotifs(ctx, ev.UserID); err != nil {
			return err
		}

		data := struct {
			DeletedAt    string
			SupportEmail string
		}{
			DeletedAt:    time.Now().Format("January 2, 2006 at 15:04 UTC"),
			SupportEmail: "", // TODO: maybe see if we have a support email, for now we have nothing so lets put nothing
		}

		return email.Send("account_deleted", ev.Email, "Account successfully deleted", data) // if the user deleted his account, we dont need his id because it doesnt exist anymore

	case "data.export_ready":
		ev, err := parse[DataExportReadyEvent](d)
		if err != nil {
			return err
		}

		expires, err := time.Parse(time.RFC3339, ev.ExpiresAt)
		if err != nil {
			return fmt.Errorf("parse expires_at %q: %w", ev.ExpiresAt, err)
		}

		data := struct {
			DownloadURL string
			ExpiresAt   string
		}{
			DownloadURL: ev.DownloadURL,
			ExpiresAt:   expires.Format("January 2, 2006 at 15:04 UTC"),
		}

		return email.Send("data_export_ready", ev.Email, "Your data export is ready", data)

	default:
		log.Printf("unknown routing key: %s", d.RoutingKey)
		return nil
	}
}

// Decode a delivery into the struct [T] and return an error if a field is wrong.
func decode[T any](d amqp.Delivery) (T, error) {
	var ev T
	dec := json.NewDecoder(bytes.NewReader(d.Body))
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
		return ev, fmt.Errorf("parsing: %w: %s", failure.FailPermanent, err)
	}
	if err := ev.Validate(); err != nil {
		return ev, fmt.Errorf("parsing: %w: %s", failure.FailPermanent, err)
	}
	return ev, nil
}
