using Guild.Application.Abstractions;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeInviteCodeGenerator(string seed = "seed00") : IInviteCodeGenerator
{
	private int _counter;

	public string NextCode() => $"{seed}{++_counter:D2}";
}
