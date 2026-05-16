namespace Guild.Domain.Guild;

[Flags]
public enum Permission : long
{
	None = 0L,
	SendMessages = 1L << 0,   // 1
	ReadMessages = 1L << 1,   // 2
	ManageMessages = 1L << 2,   // 4
	ManageChannels = 1L << 3,   // 8
	KickMembers = 1L << 4,   // 16
	BanMembers = 1L << 5,   // 32
	ManageRoles = 1L << 6,   // 64
	ManageGuild = 1L << 7,   // 128
	Administrator = 1L << 8,   // 256
	CreateInvite = 1L << 9,   // 512
	MentionEveryone = 1L << 10,  // 1024
	ManageNicknames = 1L << 11,  // 2048
}
