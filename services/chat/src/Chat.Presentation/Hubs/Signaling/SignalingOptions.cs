namespace Chat.Presentation.Hubs.Signaling;

public sealed class SignalingOptions
{
	public const string SectionName = "Signaling";

	// how long a call may ring unanswered before the caller gets CallFailed:timeout
	public int AnswerTimeoutSeconds { get; set; } = 120;
}
