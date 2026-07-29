namespace Bcmp.Api.Authorization;

public static class AuthorizationPolicyNames
{
    public const string RequireAdministrator = "RequireAdministrator";
    public const string RequireTrustee = "RequireTrustee";
    public const string RequireJobLoad = "RequireJobLoad";
    public const string RequireJobCreate = "RequireJobCreate";
    public const string RequireJobStatusUpdate = "RequireJobStatusUpdate";
    public const string RequireJobAssign = "RequireJobAssign";
}
