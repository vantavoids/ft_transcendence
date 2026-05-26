using System.Reflection;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class RoleTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	// --- public Create() factory ---

	[Fact]
	public void Create_HappyPath_PersistsFields()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "Moderator", color: null,
			permissions: 42L, position: 3,
			isDefault: false, isHoisted: true, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
		var role = result.Value;
		Assert.Equal(1L, role.Id);
		Assert.Equal(10L, role.GuildId);
		Assert.Equal("Moderator", role.Name);
		Assert.Null(role.Color);
		Assert.Equal(42L, role.Permissions);
		Assert.Equal(3, role.Position);
		Assert.False(role.IsDefault);
		Assert.True(role.IsHoisted);
		Assert.False(role.IsMentionable);
		Assert.Equal(Now, role.CreatedAt);
		Assert.Equal(Now, role.UpdatedAt);
	}

	[Fact]
	public void Create_NullName_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: null!, color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidRoleName, result.Error);
	}

	[Fact]
	public void Create_EmptyName_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "", color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidRoleName, result.Error);
	}

	[Fact]
	public void Create_NameTooLong_Fails()
	{
		var name = new string('a', Role.MaxNameLen + 1);

		var result = Role.Create(
			id: 1, guildId: 10, name: name, color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidRoleName, result.Error);
	}

	[Fact]
	public void Create_NameWithControlChar_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "bad\nname", color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidRoleName, result.Error);
	}

	[Fact]
	public void Create_NameAtMaxLength_Succeeds()
	{
		var name = new string('a', Role.MaxNameLen);

		var result = Role.Create(
			id: 1, guildId: 10, name: name, color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
	}

	// --- color validation ---

	[Fact]
	public void Create_ValidUppercaseHexColor_Succeeds()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#1A2B3C",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("#1A2B3C", result.Value.Color);
	}

	[Fact]
	public void Create_ValidLowercaseHexColor_Succeeds()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#aabbcc",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void Create_ValidMixedCaseHexColor_Succeeds()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#aAbBcC",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void Create_ValidHexColor_AtBoundaryChars_Succeeds()
	{
		// pins the inclusive hex boundaries 0, 9, f, and F. without these in any
		// valid-color test, a mutation flipping `>= '0'` to `> '0'` would silently
		// reject them; same for 9 against `<= '9'`, f against `<= 'f'`, F against
		// `<= 'F'`. one char is enough per boundary, packed into a single string
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#0F90fF",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void Create_NullColor_Succeeds()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: null,
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.Color);
	}

	[Fact]
	public void Create_InvalidColor_NoHash_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "1A2B3C",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidColor, result.Error);
	}

	[Fact]
	public void Create_InvalidColor_TooShort_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#12345",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidColor, result.Error);
	}

	[Fact]
	public void Create_InvalidColor_TooLong_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#1234567",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidColor, result.Error);
	}

	[Fact]
	public void Create_InvalidColor_NonHexChar_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "#GG2B3C",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidColor, result.Error);
	}

	[Fact]
	public void Create_InvalidColor_EmptyString_Fails()
	{
		var result = Role.Create(
			id: 1, guildId: 10, name: "R", color: "",
			permissions: 0L, position: 0,
			isDefault: false, isHoisted: false, isMentionable: false, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InvalidColor, result.Error);
	}

	// --- seeded role factories ---

	[Fact]
	public void CreateEveryone_HasCorrectShape()
	{
		var role = InvokeFactory("CreateEveryone", id: 1, guildId: 10, Now);

		Assert.True(role.IsDefault);
		Assert.Equal(515L, role.Permissions);
		Assert.Equal(0, role.Position);
		Assert.Equal("@everyone", role.Name);
		Assert.Equal(10L, role.GuildId);
	}

	[Fact]
	public void CreateEveryone_IsNotHoisted_IsNotMentionable()
	{
		var role = InvokeFactory("CreateEveryone", id: 1, guildId: 10, Now);

		Assert.False(role.IsHoisted);
		Assert.False(role.IsMentionable);
	}

	[Fact]
	public void CreateAdministrator_HasCorrectShape()
	{
		var role = InvokeFactory("CreateAdministrator", id: 2, guildId: 10, Now);

		Assert.False(role.IsDefault);
		Assert.Equal(256L, role.Permissions);
		Assert.Equal(1, role.Position);
		Assert.Equal("Administrator", role.Name);
		Assert.Equal(10L, role.GuildId);
	}

	[Fact]
	public void CreateAdministrator_IsNotHoisted_IsNotMentionable()
	{
		var role = InvokeFactory("CreateAdministrator", id: 2, guildId: 10, Now);

		Assert.False(role.IsHoisted);
		Assert.False(role.IsMentionable);
	}

	// both internal factories reached via reflection; avoids adding InternalsVisibleTo to Guild.Domain
	private static Role InvokeFactory(string methodName, long id, long guildId, DateTimeOffset now)
	{
		var method = typeof(Role).GetMethod(
			methodName,
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

		return (Role)method.Invoke(null, [id, guildId, now])!;
	}
}
