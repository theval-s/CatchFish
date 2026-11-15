using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public enum GoogleSignInState
{
    Idle,
    Waiting,  // the player is signing in in the browser
    Conflict, // the Google account already has another player
}

/// <summary>
/// The player's account: a guest on this device, optionally linked to Google. Linking opens Google's page
/// in the browser and polls the server until the player is done there.
/// </summary>
public class AccountService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    // The server forgets a sign-in after 10 minutes.
    private const int MaxPolls = 300;

    private readonly IApiClient _api;
    private readonly ProfileSyncService _profile;
    private readonly BrowserService _browser;
    private CancellationTokenSource _polling;
    private string _loginId;
    private string _loginSecret;

    public AccountService(IApiClient api, ProfileSyncService profile, BrowserService browser)
    {
        _api = api;
        _profile = profile;
        _browser = browser;
    }

    public event Action Changed;

    /// <summary>Raised after this device switched to the Google account's player and loaded its profile.</summary>
    public event Action PlayerSwitched;

    public GoogleSignInState State { get; private set; }

    /// <summary>The linked Google account, or null for a guest.</summary>
    public string GoogleEmail { get; private set; }

    /// <summary>The Google account that already has a player, while <see cref="State"/> is Conflict.</summary>
    public string ConflictEmail { get; private set; }

    /// <summary>Why the last sign-in didn't work, or null.</summary>
    public string LastError { get; private set; }

    public async Task LoadAsync()
    {
        try
        {
            GoogleEmail = (await _api.GetAccountAsync()).Google;
            Changed?.Invoke();
        }
        catch (ApiException e)
        {
            UnityEngine.Debug.LogWarning($"Account: {e.Message}");
        }
    }

    public async Task StartGoogleAsync()
    {
        if (State == GoogleSignInState.Waiting)
        {
            return;
        }

        LastError = null;
        try
        {
            _loginSecret = RandomHex(32);
            var start = await _api.BeginGoogleSignInAsync(_loginSecret);
            _loginId = start.LoginId;
            _browser.Open(start.Url);
        }
        catch (ApiException e)
        {
            Fail(e.Code == "google_off" ? "Google sign-in isn't set up on this server." : e.Detail);
            return;
        }

        SetState(GoogleSignInState.Waiting);
        _polling?.Cancel();
        _polling = new CancellationTokenSource();
        await PollAsync(_polling.Token);
    }

    /// <summary>Gives up waiting, or keeps the guest instead of switching.</summary>
    public void Cancel()
    {
        _polling?.Cancel();
        SetState(GoogleSignInState.Idle);
    }

    /// <summary>After a conflict: plays as the Google account's player on this device from now on.</summary>
    public async Task SwitchPlayerAsync()
    {
        if (State != GoogleSignInState.Conflict)
        {
            return;
        }

        try
        {
            await CompleteAsync(await _api.FinishGoogleSignInAsync(_loginId, _loginSecret, true));
        }
        catch (ApiException e)
        {
            Fail(e.Detail);
        }
    }

    private async Task PollAsync(CancellationToken cancellation)
    {
        for (var i = 0; i < MaxPolls; i++)
        {
            await Task.Delay(PollInterval);
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var result = await _api.FinishGoogleSignInAsync(_loginId, _loginSecret, false);
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }
                if (result != null)
                {
                    await CompleteAsync(result);
                    return;
                }
            }
            catch (ApiException e) when (e.Code == ApiException.ConnectionError)
            {
                // The phone may be offline for a moment while the browser is in front: keep trying.
            }
            catch (ApiException e) when (e.Code == "identity_taken")
            {
                ConflictEmail = e.Detail;
                SetState(GoogleSignInState.Conflict);
                return;
            }
            catch (ApiException e)
            {
                Fail(e.Detail);
                return;
            }
        }

        Fail("Google sign-in took too long. Try again.");
    }

    private async Task CompleteAsync(GoogleSignInResult result)
    {
        GoogleEmail = result.Email;
        ConflictEmail = null;
        SetState(GoogleSignInState.Idle);
        if (result.SwitchedPlayer && await _profile.TryRefreshAsync())
        {
            PlayerSwitched?.Invoke();
        }
    }

    private void Fail(string error)
    {
        LastError = error;
        SetState(GoogleSignInState.Idle);
    }

    private void SetState(GoogleSignInState state)
    {
        State = state;
        Changed?.Invoke();
    }

    private static string RandomHex(int bytes)
    {
        var data = new byte[bytes];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(data);
        }

        var text = new StringBuilder(bytes * 2);
        foreach (var b in data)
        {
            text.Append(b.ToString("x2"));
        }

        return text.ToString();
    }
}
