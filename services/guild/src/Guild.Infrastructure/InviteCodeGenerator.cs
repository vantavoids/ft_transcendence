using System.Security.Cryptography;
using Guild.Application.Abstractions;

namespace Guild.Infrastructure;

/// <summary>
/// emits 10-char URL-safe codes drawn from a 56-symbol alphabet (no easily
/// confused glyphs like 0/O/I/l/1). roughly 2^58 of entropy per code, well
/// above what a casual brute force can scan against a guild_invites primary
/// key with a non-existent code yielding a cheap 404
/// </summary>
internal sealed class InviteCodeGenerator : IInviteCodeGenerator
{
	private const string Alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
	private const int Length = 10;

	public string NextCode()
	{
		var buffer = RandomNumberGenerator.GetBytes(Length);
		var chars = new char[Length];
		for (var i = 0; i < Length; i++)
			chars[i] = Alphabet[buffer[i] % Alphabet.Length];
		return new string(chars);
	}
}
