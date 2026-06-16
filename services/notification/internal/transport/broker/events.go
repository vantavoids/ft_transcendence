package broker

type ChatMessageSentEvent struct {
	ChannelID int64    `json:"channel_id,string"`
	GuildID   int64    `json:"guild_id,string"`
	AuthorID  int64    `json:"author_id,string"`
	MessageID int64    `json:"message_id,string"`
	Content   string   `json:"content"`
	Mentions  []string `json:"mentions"` // This is intended, json.Unmarshal can't parse an array of string into int64, the convertion has to be made in the dispatch
}

type ChatDmSentEvent struct {
	ConversationID int64  `json:"conversation_id,string"`
	MessageID      int64  `json:"message_id,string"`
	SenderID       int64  `json:"sender_id,string"`
	RecipientID    int64  `json:"recipient_id,string"`
	Content        string `json:"content"`
}

type FriendRequestSentEvent struct {
	FriendshipID int64 `json:"friendship_id,string"`
	RequesterID  int64 `json:"requester_id,string"`
	AddresseeID  int64 `json:"addressee_id,string"`
}

type GuildInviteCreatedEvent struct {
	GuildID         int64  `json:"guild_id,string"`
	GuildName       string `json:"guild_name"`
	InvitedByUserID int64  `json:"invited_by_user_id,string"`
	InvitedUserID   int64  `json:"invited_user_id,string"`
}

type GuildMemberJoinedEvent struct {
	GuildID   int64  `json:"guild_id,string"`
	GuildName string `json:"guild_name"`
	UserID    int64  `json:"user_id,string"`
}

type CallIncomingEvent struct {
	CallID   int64  `json:"call_id,string"`
	CallerID int64  `json:"caller_id,string"`
	CalleeID int64  `json:"callee_id,string"`
	CallType string `json:"call_type"`
}
