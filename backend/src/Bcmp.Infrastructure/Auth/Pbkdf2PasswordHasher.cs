using System.Security.Cryptography;
using Bcmp.Application.Auth;

namespace Bcmp.Infrastructure.Auth;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 210_000;
    private const string Format = "PBKDF2-SHA256";

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return string.Join('$', Format, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Format || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                providedPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
