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
        // Assets may not be imported yet while the editor is still loading.
        EditorApplication.delayCall += () =>
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
    }
}
