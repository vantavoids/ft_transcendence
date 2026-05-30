namespace Guild.Domain.Guild;

/// <summary>
/// the kind of channel. persisted as int via EF so adding new variants stays a
/// non-breaking column-level change. NOT a [Flags] enum
/// </summary>
public enum ChannelType
{
	Text = 0,
	Announcement = 1
}
