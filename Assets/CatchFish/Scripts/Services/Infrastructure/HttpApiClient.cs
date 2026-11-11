using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Talks to the C++ server in Server/ over HTTP + JSON (the API is in Server/README.md). Signs in as
/// this device's guest: the guest secret is kept in PlayerPrefs, the session only in memory.
/// </summary>
public class HttpApiClient : IApiClient
{
    private const string GuestSecretKey = "CatchFish.GuestSecret";
    // Where the first server version's token was kept; that server turned it into a guest secret.
    private const string OldTokenKey = "CatchFish.ServerToken";
    private const int TimeoutSeconds = 10;

    private readonly string _baseUrl;
    private string _session;

    public HttpApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    private static string GuestSecret
    {
        get => PlayerPrefs.GetString(GuestSecretKey, "");
        set
        {
            PlayerPrefs.SetString(GuestSecretKey, value);
            PlayerPrefs.Save();
        }
    }

    public async Task SignInAsync()
    {
        TakeOverOldToken();
        if (string.IsNullOrEmpty(GuestSecret))
        {
            await CreateGuestAsync();
            return;
        }

        try
        {
            var body = await SendAsync("POST", "/auth/guest/login", new { guestSecret = GuestSecret }, null);
            _session = Json.Deserialize<SessionResponse>(body).SessionToken;
        }
        catch (ApiException e) when (e.Code == "invalid_credentials")
        {
            // Not replaced with a new guest here: if the server is just pointed at the wrong
            // database, that would lose this player for good. The player decides (BootstrapFlow).
            throw new ApiException(ApiException.AccountNotFound, "the server doesn't know this device's player");
        }
    }

    public Task StartNewPlayerAsync() => CreateGuestAsync();

    public async Task<GameConfig> LoadConfigAsync() =>
        Json.Deserialize<GameConfig>(await SendAsync("GET", "/config", null, null));

    public Task<GameState> LoadProfileAsync() => SendSignedInAsync<GameState>("GET", "/profile");

    public async Task<DateTime> GetServerTimeAsync() =>
        Json.Deserialize<TimeResponse>(await SendAsync("GET", "/time", null, null)).Now;

    public Task<GameState> BuyGearAsync(string gearId) =>
        SendSignedInAsync<GameState>("POST", "/gear/buy", new { gearId });

    public Task<GameState> CompleteQuestAsync(int questId) =>
        SendSignedInAsync<GameState>("POST", "/quests/complete", new { questId });

    public Task<GameState> StartRunAsync() => SendSignedInAsync<GameState>("POST", "/runs/start");

    public Task<GameState> CompleteRunAsync(RunResult result) =>
        SendSignedInAsync<GameState>("POST", "/runs/complete", result);

    public Task<GameState> ClaimDailyRewardAsync() => SendSignedInAsync<GameState>("POST", "/daily/claim");

    private async Task<T> SendSignedInAsync<T>(string method, string path, object body = null)
    {
        if (_session == null)
        {
            await SignInAsync();
        }

        try
        {
            return Json.Deserialize<T>(await SendAsync(method, path, body, _session));
        }
        catch (ApiException e) when (e.Code == "session_expired" || e.Code == "unauthorized")
        {
            // Sessions run out after 30 days (or the server forgot them): the guest secret gets a new one.
            _session = null;
            await SignInAsync();
            return Json.Deserialize<T>(await SendAsync(method, path, body, _session));
        }
    }

    private async Task CreateGuestAsync()
    {
        var guest = Json.Deserialize<GuestResponse>(await SendAsync("POST", "/auth/guest", null, null));
        GuestSecret = guest.GuestSecret;
        _session = guest.SessionToken;
    }

    private static void TakeOverOldToken()
    {
        if (!PlayerPrefs.HasKey(OldTokenKey))
        {
            return;
        }

        if (string.IsNullOrEmpty(GuestSecret))
        {
            GuestSecret = PlayerPrefs.GetString(OldTokenKey);
        }

        PlayerPrefs.DeleteKey(OldTokenKey);
        PlayerPrefs.Save();
    }

    private async Task<string> SendAsync(string method, string path, object body, string session)
    {
        var url = _baseUrl + path;
        using (var request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = TimeoutSeconds;

            // A POST always gets a body: without a Content-Length the server's HTTP library
            // waits for one until it times out.
            if (body != null || method == "POST")
            {
                var json = body != null ? Json.Serialize(body) : "{}";
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (session != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + session);
            }

            await request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    return request.downloadHandler.text;
                case UnityWebRequest.Result.ProtocolError:
                    throw ToApiException(request);
                default:
                    throw new ApiException(ApiException.ConnectionError, $"{method} {url}: {request.error}");
            }
        }
    }

    // The server answers errors with { "error": code, "message": text }.
    private static ApiException ToApiException(UnityWebRequest request)
    {
        try
        {
            var error = Json.Deserialize<ErrorResponse>(request.downloadHandler.text);
            if (error?.Error != null)
            {
                return new ApiException(error.Error, error.Message);
            }
        }
        catch (JsonException)
        {
            // Not from our server: fall back to the status code.
        }

        return new ApiException($"http_{request.responseCode}", $"{request.method} {request.url}: {request.error}");
    }

    private class TimeResponse
    {
        public DateTime Now { get; set; }
    }

    private class SessionResponse
    {
        public long PlayerId { get; set; }
        public string SessionToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private class GuestResponse : SessionResponse
    {
        public string GuestSecret { get; set; }
    }

    private class ErrorResponse
    {
        public string Error { get; set; }
        public string Message { get; set; }
    }
}
