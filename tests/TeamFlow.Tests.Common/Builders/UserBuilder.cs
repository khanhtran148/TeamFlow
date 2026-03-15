using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class UserBuilder
{
    private string _email = "test@teamflow.dev";
    private string _name = "Test User";
    private string _passwordHash = "hashed_Test@1234";

    public static UserBuilder New() => new();

    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }

    public User Build() => new()
    {
        Email = _email,
        Name = _name,
        PasswordHash = _passwordHash
    };
}
