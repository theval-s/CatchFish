using System;
using VContainer.Unity;

/// <summary>Runs the account popup: links a Google account, and reloads the hub after switching players.</summary>
public class AccountPresenter : IStartable, IDisposable
{
    private readonly AccountView _view;
    private readonly AccountService _account;
    private readonly SceneLoader _scenes;

    public AccountPresenter(AccountView view, AccountService account, SceneLoader scenes)
    {
        _view = view;
        _account = account;
        _scenes = scenes;
    }

    public void Start()
    {
        _view.Opened += OnOpened;
        _view.GoogleClicked += OnGoogle;
        _view.SwitchClicked += OnSwitch;
        _view.CancelClicked += _account.Cancel;
        _account.Changed += Refresh;
        _account.PlayerSwitched += OnPlayerSwitched;
        Refresh();
    }

    public void Dispose()
    {
        _view.Opened -= OnOpened;
        _view.GoogleClicked -= OnGoogle;
        _view.SwitchClicked -= OnSwitch;
        _view.CancelClicked -= _account.Cancel;
        _account.Changed -= Refresh;
        _account.PlayerSwitched -= OnPlayerSwitched;
    }

    private void OnOpened()
    {
        if (_account.State == GoogleSignInState.Idle)
        {
            _ = _account.LoadAsync();
        }
    }

    private void OnGoogle() => _ = _account.StartGoogleAsync();

    private void OnSwitch() => _ = _account.SwitchPlayerAsync();

    // Every view shows the old player's state: start the hub over with the new one.
    private void OnPlayerSwitched() => _ = _scenes.LoadAsync(SceneNames.Hub);

    private void Refresh()
    {
        switch (_account.State)
        {
            case GoogleSignInState.Waiting:
                _view.Show("Finish signing in with Google in your browser, then come back here.", false, false, true);
                break;
            case GoogleSignInState.Conflict:
                _view.Show($"{_account.ConflictEmail} already has a CatchFish player.\n" +
                           "Play as that player on this device? This guest's progress will be left behind.",
                    false, true, true);
                break;
            default:
                var error = _account.LastError != null ? $"\n\n{_account.LastError}" : "";
                if (_account.GoogleEmail != null)
                {
                    _view.Show($"Signed in with Google:\n{_account.GoogleEmail}\n\nYour progress is saved to this account." + error,
                        false, false, false);
                }
                else
                {
                    _view.Show("Playing as a guest: your progress lives on this device only.\n" +
                               "Continue with Google to keep it and play on other devices." + error, true, false, false);
                }
                break;
        }
    }
}
