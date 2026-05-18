namespace Chat.Application.Abstractions;

public interface ISnowflakeIdGenerator
{
	long NextId();
}
