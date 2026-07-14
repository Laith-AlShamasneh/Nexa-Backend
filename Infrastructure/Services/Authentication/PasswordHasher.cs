using Application.Interfaces.Services;

namespace Infrastructure.Services.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    // Cost is embedded in each hash, so raising this only affects newly-created
    // hashes; existing lower-cost hashes still verify. 12 ≈ a sensible 2026 baseline.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
}
