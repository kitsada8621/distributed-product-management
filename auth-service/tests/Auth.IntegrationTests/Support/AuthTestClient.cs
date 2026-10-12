using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Auth.IntegrationTests.Support;

public sealed record LoginSession(string AccessToken, string RefreshToken, string CsrfToken);

public static class AuthTestClient
{
    public const string DefaultPassword = "Passw0rd!";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = DefaultPassword)
    {
        return client.PostAsJsonAsync("/api/auth/register", new { email, password, name = "Somchai" });
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = DefaultPassword)
    {
        return client.PostAsJsonAsync("/api/auth/login", new { email, password });
    }

    public static async Task<LoginSession> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var register = await RegisterAsync(client, email);
        register.EnsureSuccessStatusCode();
        return await LoginSessionAsync(client, email, DefaultPassword);
    }

    public static async Task<LoginSession> LoginSessionAsync(HttpClient client, string email, string password)
    {
        var response = await LoginAsync(client, email, password);
        response.EnsureSuccessStatusCode();
        return await ReadSessionAsync(response);
    }

    public static async Task<LoginSession> ReadSessionAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        return new LoginSession(accessToken, CookieValue(response, "refreshToken")!, CookieValue(response, "csrfToken")!);
    }

    public static string? CookieValue(HttpResponseMessage response, string name)
    {
        var prefix = name + "=";
        var header = SetCookieHeaders(response).FirstOrDefault(h => h.StartsWith(prefix, StringComparison.Ordinal));
        return header?.Split(';')[0][prefix.Length..];
    }

    public static string? SetCookieHeader(HttpResponseMessage response, string name)
    {
        return SetCookieHeaders(response).FirstOrDefault(h => h.StartsWith(name + "=", StringComparison.Ordinal));
    }

    public static Task<HttpResponseMessage> PostWithCookiesAsync(
        HttpClient client,
        string url,
        string? refreshCookie,
        string? csrfCookie,
        string? csrfHeader,
        string? origin = AuthApiFactory.SpaOrigin,
        string? bearer = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var cookies = new List<string>();
        if (refreshCookie is not null)
        {
            cookies.Add($"refreshToken={refreshCookie}");
        }

        if (csrfCookie is not null)
        {
            cookies.Add($"csrfToken={csrfCookie}");
        }

        if (cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        }

        if (csrfHeader is not null)
        {
            request.Headers.Add("X-CSRF-Token", csrfHeader);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, LoginSession session)
    {
        return PostWithCookiesAsync(client, "/api/auth/refresh", session.RefreshToken, session.CsrfToken, session.CsrfToken);
    }

    private static IEnumerable<string> SetCookieHeaders(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];
    }
}
