using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class UserBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private string _email = F.Internet.Email(provider: "example.com");
    private string _name = F.Name.FullName();
    private string _passwordHash = "hashed_Test@1234";
    private SystemRole _systemRole = SystemRole.User;
    private bool _mustChangePassword = false;
    private bool _isActive = true;

    public static UserBuilder New() => new();

    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserBuilder WithSystemRole(SystemRole role) { _systemRole = role; return this; }
    public UserBuilder WithMustChangePassword(bool value) { _mustChangePassword = value; return this; }
    public UserBuilder WithIsActive(bool value) { _isActive = value; return this; }

    public User Build() => new()
    {
        Email = _email,
        Name = _name,
        PasswordHash = _passwordHash,
        SystemRole = _systemRole,
        MustChangePassword = _mustChangePassword,
        IsActive = _isActive
    };
}
