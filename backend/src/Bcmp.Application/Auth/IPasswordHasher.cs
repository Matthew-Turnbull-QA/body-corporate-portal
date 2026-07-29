namespace Bcmp.Application.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string providedPassword);
}
