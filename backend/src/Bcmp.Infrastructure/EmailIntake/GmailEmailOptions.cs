namespace Bcmp.Infrastructure.EmailIntake;

public sealed class GmailEmailOptions
{
    public const string SectionName = "EmailIntake:Gmail";

    public string Address { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string OAuthRedirectUri { get; init; } = "http://127.0.0.1:53682/";
    public string ImapHost { get; init; } = "imap.gmail.com";
    public int ImapPort { get; init; } = 993;
    public string SmtpHost { get; init; } = "smtp.gmail.com";
    public int SmtpPort { get; init; } = 587;
    public bool AllowInvalidServerCertificate { get; init; }
}
