using TMPro;
using UnityEngine;

/// <summary>"+$12" text that floats up and fades out where a fish was shot.</summary>
public class ShotPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 100f;

    private RectTransform _rect;
    private Color _color;
    private float _age;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _color = label.color;
    }

    public void Show(string text) => label.text = text;

    private void Update()
    {
        _age += Time.deltaTime;
        _rect.anchoredPosition += Vector2.up * (riseSpeed * Time.deltaTime);

        var color = _color;
        color.a = 1f - _age / lifetime;
        label.color = color;

        if (_age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
