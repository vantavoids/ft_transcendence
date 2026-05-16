namespace Guild.Presentation.Endpoints;

/// <summary>
/// wire shape for an error response. the <c>Error</c> property serialises as
/// <c>"error"</c> thanks to <c>JsonNamingPolicy.SnakeCaseLower</c>
/// </summary>
public sealed record ErrorBody(string Error);
