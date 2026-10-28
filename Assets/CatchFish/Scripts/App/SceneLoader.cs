using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Switches between the game's scenes. Services live in the root scope and survive the switch.</summary>
public class SceneLoader
{
    public async Awaitable LoadAsync(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName);
    }
}
