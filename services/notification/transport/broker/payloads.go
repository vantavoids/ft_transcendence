package broker

type MentionPayload struct {
	ChannelID int64  `json:"channel_id"`
	GuildID   int64  `json:"guild_id"`
	Preview   string `json:"preview"`
}

type DmPayload struct {
	ConversationID int64  `json:"conversation_id"`
	Preview        string `json:"preview"`
}

type FriendRequestPayload struct {
}

type GuildInvitePayload struct {
	GuildName string `json:"guild_name"`
}

type GuildWelcomePayload struct {
	GuildName string `json:"guild_name"`
}

// the actor (old owner) and source (guild) are already carried on the
// notification row, so the payload stays empty; the frontend resolves the
// guild name from the source id.
type GuildOwnershipTransferredPayload struct {
}

type IncomingCallPayload struct {
	CallType string `json:"call_type"`
}
