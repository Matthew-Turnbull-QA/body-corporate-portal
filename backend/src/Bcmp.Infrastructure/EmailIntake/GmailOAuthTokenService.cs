using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bcmp.Infrastructure.EmailIntake;

public sealed class GmailOAuthTokenService(IOptions<GmailEmailOptions> options)
{
    private const string GmailScope = "https://mail.google.com/";
    private static readonly HttpClient HttpClient = new();

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        ValidateRuntimeOptions(configured);

        var response = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = configured.ClientId,
                ["client_secret"] = configured.ClientSecret,
                ["refresh_token"] = configured.RefreshToken,
                ["grant_type"] = "refresh_token",
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new InvalidOperationException("Google OAuth did not return an access token.");
        }

        return response.AccessToken;
    }

    public async Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        ValidateAuthorizationOptions(configured);

        using var listener = new HttpListener();
        listener.Prefixes.Add(configured.OAuthRedirectUri);
        listener.Start();

        var authorizationUrl = BuildAuthorizationUrl(configured);
        Console.WriteLine();
        Console.WriteLine("Opening Google consent in your browser...");
        Console.WriteLine();
        Console.WriteLine(authorizationUrl);
        Console.WriteLine();

        Process.Start(new ProcessStartInfo
        {
            FileName = authorizationUrl,
            UseShellExecute = true,
        });

        using var registration = cancellationToken.Register(() =>
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        });

        var context = await listener.GetContextAsync();
        var request = context.Request;
        var code = request.QueryString["code"];
        var error = request.QueryString["error"];

        await WriteBrowserResponseAsync(context.Response, string.IsNullOrWhiteSpace(error), cancellationToken);
        listener.Stop();

        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"Google OAuth authorization failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Google OAuth did not return an authorization code.");
        }

        var tokenResponse = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = configured.ClientId,
                ["client_secret"] = configured.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = configured.OAuthRedirectUri,
                ["grant_type"] = "authorization_code",
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException(
                "Google OAuth did not return a refresh token. Revoke the app's access from the Gmail account, then run --gmail-oauth again.");
        }

        return tokenResponse.RefreshToken;
    }

    private static string BuildAuthorizationUrl(GmailEmailOptions options)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.OAuthRedirectUri,
            ["response_type"] = "code",
            ["scope"] = GmailScope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["login_hint"] = options.Address,
        };

        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static async Task<TokenResponse> RequestTokenAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(values);
        using var response = await HttpClient.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = tokenResponse?.ErrorDescription ?? tokenResponse?.Error ?? response.ReasonPhrase ?? "Unknown OAuth error.";
            throw new InvalidOperationException($"Google OAuth token request failed: {message}");
        }

        return tokenResponse ?? throw new InvalidOperationException("Google OAuth token response was empty.");
    }

    private static async Task WriteBrowserResponseAsync(
        HttpListenerResponse response,
        bool success,
        CancellationToken cancellationToken)
    {
        var html = success
            ? "<html><body><h1>Gmail OAuth complete</h1><p>You can return to the terminal.</p></body></html>"
            : "<html><body><h1>Gmail OAuth failed</h1><p>Return to the terminal for details.</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
        response.Close();
    }

    private static void ValidateAuthorizationOptions(GmailEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Address)
            || string.IsNullOrWhiteSpace(options.ClientId)
            || string.IsNullOrWhiteSpace(options.ClientSecret)
            || string.IsNullOrWhiteSpace(options.OAuthRedirectUri))
        {
            throw new InvalidOperationException(
                "EmailIntake:Gmail:Address, ClientId, ClientSecret, and OAuthRedirectUri must be configured before running --gmail-oauth.");
        }
    }

    private static void ValidateRuntimeOptions(GmailEmailOptions options)
    {
        ValidateAuthorizationOptions(options);

        if (string.IsNullOrWhiteSpace(options.RefreshToken))
        {
            throw new InvalidOperationException(
                "EmailIntake:Gmail:RefreshToken is missing. Run dotnet run --project src/Bcmp.Api -- --gmail-oauth, then save the printed refresh token.");
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);
}
