using Bogus;

namespace TeamFlow.Tests.Common.Fakers;

internal static class FakerProvider
{
    private static readonly ThreadLocal<Faker> _faker = new(() => new Faker());

    public static Faker Instance => _faker.Value!;
}
