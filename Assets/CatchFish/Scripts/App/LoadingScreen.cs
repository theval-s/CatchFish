using UnityEngine;

/// <summary>
/// Full-screen overlay that blocks input while <see cref="SceneLoader"/> switches scenes.
/// Lives in RootScope.prefab, so it survives scene changes; the panel starts visible to cover app startup too.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public void Show() => panel.SetActive(true);

    public void Hide() => panel.SetActive(false);
}
