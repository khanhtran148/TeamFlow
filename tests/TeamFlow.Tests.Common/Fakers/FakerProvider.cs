using Bogus;

namespace TeamFlow.Tests.Common.Fakers;

public static class FakerProvider
{
    private static readonly Lock Lock = new();

    public static Faker Instance
    {
        get
        {
            lock (Lock)
            {
                return new Faker();
            }
        }
    }
}
