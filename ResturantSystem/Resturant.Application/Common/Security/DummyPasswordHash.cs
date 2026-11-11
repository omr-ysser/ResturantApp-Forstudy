using Restaurant.Application.Abstractions.Authentication;

namespace Restaurant.Application.Common.Security;

public sealed class DummyPasswordHash
{
    public DummyPasswordHash(
        IPasswordHasher passwordHasher)
    {
        Value = passwordHasher.Hash(
            "This-is-a-dummy-password-not-an-account");
    }

    public string Value { get; }
}