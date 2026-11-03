using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Switches between the game's scenes. Services live in the root scope and survive the switch.</summary>
public class SceneLoader
{
    private readonly LoadingScreen _loadingScreen;
    private bool _isLoading;

    public SceneLoader(LoadingScreen loadingScreen)
    {
        _loadingScreen = loadingScreen;
    }

    public async Awaitable LoadAsync(string sceneName)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        _loadingScreen.Show();
        try
        {
            // Let the overlay render before starting the potentially expensive scene switch.
            await Awaitable.NextFrameAsync();
            await SceneManager.LoadSceneAsync(sceneName);
            await Awaitable.NextFrameAsync();
        }
        finally
        {
            _loadingScreen.Hide();
            _isLoading = false;
        }
    }
}
