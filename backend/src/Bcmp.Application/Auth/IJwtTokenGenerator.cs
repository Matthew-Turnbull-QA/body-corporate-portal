using Bcmp.Domain.Users;

namespace Bcmp.Application.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
