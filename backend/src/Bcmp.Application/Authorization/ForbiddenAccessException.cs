namespace Bcmp.Application.Authorization;

public sealed class ForbiddenAccessException(string message) : Exception(message);
