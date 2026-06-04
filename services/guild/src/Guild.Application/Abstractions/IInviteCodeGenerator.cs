namespace Guild.Application.Abstractions;

public interface IInviteCodeGenerator
{
	/// <summary>
	/// returns an opaque URL-safe invite code. callers must not parse the value
	/// or rely on its format beyond "shorter than the schema column".
	/// </summary>
	string NextCode();
}
