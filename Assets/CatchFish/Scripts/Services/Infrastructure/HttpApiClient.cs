using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Talks to the C++ server in Server/ over HTTP + JSON (the API is in Server/README.md).
/// Signs in as a guest the first time and keeps the token in PlayerPrefs.
/// </summary>
public class HttpApiClient : IApiClient
{
    private const string TokenKey = "CatchFish.ServerToken";
    private const int TimeoutSeconds = 10;

    private readonly string _baseUrl;

    public HttpApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    private static string Token
    {
        get => PlayerPrefs.GetString(TokenKey, "");
        set
        {
            PlayerPrefs.SetString(TokenKey, value);
            PlayerPrefs.Save();
        }
    }

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

    private async Task<T> SendSignedInAsync<T>(string method, string path, object body = null)
    {
        if (string.IsNullOrEmpty(Token))
        {
            await SignInAsync();
        }

        try
        {
            return Json.Deserialize<T>(await SendAsync(method, path, body, Token));
        }
        catch (ApiException e) when (e.Code == "unauthorized")
        {
            Debug.LogWarning("The server doesn't know this player (was its database deleted?). Starting as a new player.");
            await SignInAsync();
            return Json.Deserialize<T>(await SendAsync(method, path, body, Token));
        }
    }

    private async Task SignInAsync()
    {
        Token = Json.Deserialize<TokenResponse>(await SendAsync("POST", "/auth/guest", null, null)).Token;
    }

    private async Task<string> SendAsync(string method, string path, object body, string token)
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

            if (token != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            await request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    return request.downloadHandler.text;
                case UnityWebRequest.Result.ProtocolError:
                    throw ToApiException(request);
                default:
                    throw new ApiException("connection_error", $"{method} {url}: {request.error}");
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

    private class TokenResponse
    {
        public string Token { get; set; }
    }

    private class ErrorResponse
    {
        public string Error { get; set; }
        public string Message { get; set; }
    }
}
