using TMPro;
using UnityEngine;

/// <summary>Corner HUD during a run, plus the "+$12" popups where fish are shot.</summary>
public class FishingHudView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text hitsText;
    [SerializeField] private TMP_Text catchText;
    [SerializeField] private TMP_Text earnedText;

    [Header("Shot popups")]
    [SerializeField] private ShotPopup popupPrefab;
    [SerializeField] private RectTransform popupLayer;

    private Camera _camera;

    private void Awake() => _camera = Camera.main;

    public void SetVisible(bool visible) => panel.SetActive(visible);

    public void Show(string phase, float depth, float maxDepth, int hitsLeft, int caught, int capacity, int earned)
    {
        phaseText.text = phase;
        depthText.text = $"Depth: {depth:0} / {maxDepth:0} m";
        hitsText.text = $"Hits left: {hitsLeft}";
        catchText.text = $"On hook: {caught} / {capacity}";
        earnedText.text = $"Earned: {earned} DR";
    }

    public void ShowPopup(Vector2 worldPosition, string text)
    {
        var popup = Instantiate(popupPrefab, popupLayer);
        popup.transform.position = _camera.WorldToScreenPoint(worldPosition);
        popup.Show(text);
    }
}
