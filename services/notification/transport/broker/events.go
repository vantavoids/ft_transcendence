package broker

import (
	"fmt"
	"slices"
)

// TODO: instead of checking if the IDs are nil or 0, it should check if its a snowflake or not

type ChatMessageSentEvent struct {
	ChannelID int64    `json:"channel_id,string"`
	GuildID   int64    `json:"guild_id,string"`
	AuthorID  int64    `json:"author_id,string"`
	MessageID int64    `json:"message_id,string"`
	Content   string   `json:"content"`
	Mentions  []string `json:"mentions"` // This is intended, json.Unmarshal can't parse an array of string into int64, the convertion has to be made in the dispatch
}

func (e ChatMessageSentEvent) Validate() error {
	if e.ChannelID == 0 {
		return fmt.Errorf("chat.message_sent: missing channel_id")
	}
	if e.GuildID == 0 {
		return fmt.Errorf("chat.message_sent: missing guild_id")
	}
	if e.AuthorID == 0 {
		return fmt.Errorf("chat.message_sent: missing author_id")
	}
	if e.MessageID == 0 {
		return fmt.Errorf("chat.message_sent: missing message_id")
	}
	if slices.Contains(e.Mentions, "") {
		return fmt.Errorf("chat.message_sent: missing mention's id")
	}
	return nil
}

type ChatDmSentEvent struct {
	ConversationID int64  `json:"conversation_id,string"`
	MessageID      int64  `json:"message_id,string"`
	SenderID       int64  `json:"sender_id,string"`
	RecipientID    int64  `json:"recipient_id,string"`
	Content        string `json:"content"`
}

func (e ChatDmSentEvent) Validate() error {
	if e.ConversationID == 0 {
		return fmt.Errorf("chat.dm_sent: missing conversation_id")
	}
	if e.MessageID == 0 {
		return fmt.Errorf("chat.dm_sent: missing message_id")
	}
	if e.SenderID == 0 {
		return fmt.Errorf("chat.dm_sent: missing sender_id")
	}
	if e.RecipientID == 0 {
		return fmt.Errorf("chat.dm_sent: missing recipient_id")
	}
	return nil
}

type FriendRequestSentEvent struct {
	FriendshipID int64 `json:"friendship_id,string"`
	RequesterID  int64 `json:"requester_id,string"`
	AddresseeID  int64 `json:"addressee_id,string"`
}

func (e FriendRequestSentEvent) Validate() error {
	if e.FriendshipID == 0 {
		return fmt.Errorf("friend.request_sent: missing friendship_id")
	}
	if e.RequesterID == 0 {
		return fmt.Errorf("friend.request_sent: missing requester_id")
	}
	if e.AddresseeID == 0 {
		return fmt.Errorf("friend.request_sent: missing addressee_id")
	}
	return nil
}

type GuildInviteCreatedEvent struct {
	GuildID         int64  `json:"guild_id,string"`
	GuildName       string `json:"guild_name"`
	InvitedByUserID int64  `json:"invited_by_user_id,string"`
	InvitedUserID   int64  `json:"invited_user_id,string"`
}

func (e GuildInviteCreatedEvent) Validate() error {
	if e.GuildID == 0 {
		return fmt.Errorf("guild.invite_created: missing guild_id")
	}
	if e.GuildName == "" {
		return fmt.Errorf("guild.invite_created: missing guild_name")
	}
	if e.InvitedByUserID == 0 {
		return fmt.Errorf("guild.invite_created: missing invited_by_user_id")
	}
	if e.InvitedUserID == 0 {
		return fmt.Errorf("guild.invite_created: missing invited_user_id")
	}
	return nil
}

type GuildMemberJoinedEvent struct {
	GuildID   int64  `json:"guild_id,string"`
	GuildName string `json:"guild_name"`
	UserID    int64  `json:"user_id,string"`
}

func (e GuildMemberJoinedEvent) Validate() error {
	if e.GuildID == 0 {
		return fmt.Errorf("guild.member_joined: missing guild_id")
	}
	if e.GuildName == "" {
		return fmt.Errorf("guild.member_joined: missing guild_name")
	}
	if e.UserID == 0 {
		return fmt.Errorf("guild.member_joined: missing user_id")
	}
	return nil
}

type GuildOwnerTransferredEvent struct {
	GuildID    int64 `json:"guild_id,string"`
	OldOwnerID int64 `json:"old_owner_id,string"`
	NewOwnerID int64 `json:"new_owner_id,string"`
}

func (e GuildOwnerTransferredEvent) Validate() error {
	if e.GuildID == 0 {
		return fmt.Errorf("guild.owner_transferred: missing guild_id")
	}
	if e.NewOwnerID == 0 {
		return fmt.Errorf("guild.owner_transferred: missing new_owner_id")
	}
	return nil
}

type CallIncomingEvent struct {
	CallID   int64  `json:"call_id,string"`
	CallerID int64  `json:"caller_id,string"`
	CalleeID int64  `json:"callee_id,string"`
	CallType string `json:"call_type"`
}

func (e CallIncomingEvent) Validate() error {
	if e.CallID == 0 {
		return fmt.Errorf("call.incoming: missing call_id")
	}
	if e.CallerID == 0 {
		return fmt.Errorf("call.incoming: missing caller_id")
	}
	if e.CalleeID == 0 {
		return fmt.Errorf("call.incoming: missing callee_id")
	}
	if e.CallType != "audio" && e.CallType != "video" {
		return fmt.Errorf("call.incoming: wrong call_type")
	}
	return nil
}

type UserDeletedEvent struct {
	UserID int64  `json:"user_id,string"`
	Email  string `json:"email"`
}

func (e UserDeletedEvent) Validate() error {
	if e.UserID == 0 {
		return fmt.Errorf("user.deleted: missing user_id")
	}
	if e.Email == "" {
		return fmt.Errorf("user.deleted: missing email")
	}
	return nil
}

type DataExportReadyEvent struct {
	UserID      string `json:"user_id"`
	Email       string `json:"email"`
	DownloadURL string `json:"download_url"`
	ExpiresAt   string `json:"expires_at"`
}

func (e DataExportReadyEvent) Validate() error {
	if e.UserID == "" {
		return fmt.Errorf("data.export_ready: missing user_id")
	}
	if e.Email == "" {
		return fmt.Errorf("data.export_ready: missing email")
	}
	if e.DownloadURL == "" {
		return fmt.Errorf("data.export_ready: missing download_url")
	}
	if e.ExpiresAt == "" {
		return fmt.Errorf("data.export_ready: missing expires_at")
	}
	return nil
}
