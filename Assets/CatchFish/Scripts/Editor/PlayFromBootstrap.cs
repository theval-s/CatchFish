using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Play mode always starts from the Bootstrap scene, whichever scene is open,
/// so the config and profile are loaded before the hub or fishing scene runs.
/// </summary>
[InitializeOnLoad]
public static class PlayFromBootstrap
{
    private const string BootstrapPath = "Assets/CatchFish/Scenes/Bootstrap.unity";

    static PlayFromBootstrap()
    {
        // Set right before every Play: setting it once at load time could run before the scene asset
        // was imported, and then Play started from the open scene.
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
            }
        };
    }
}
