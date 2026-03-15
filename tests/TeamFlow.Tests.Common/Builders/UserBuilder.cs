using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class UserBuilder
{
    private static Faker F => FakerProvider.Instance;

    private string _email = F.Internet.Email();
    private string _name = F.Name.FullName();
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
