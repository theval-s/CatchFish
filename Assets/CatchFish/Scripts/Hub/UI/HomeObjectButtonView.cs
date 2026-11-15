using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A home object in the Home tab: its picture, name and level. Clicking it opens the upgrade popup.</summary>
public class HomeObjectButtonView : MonoBehaviour
{
    [SerializeField] private Image picture;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    private HomeSkinCatalog _skins;
    private string _objectId;

    public event Action Clicked;

    public void SetSkin(HomeSkinCatalog skins, string objectId)
    {
        _skins = skins;
        _objectId = objectId;
    }

    public void Show(string title, string levelLine, int level)
    {
        nameText.text = title;
        levelText.text = levelLine;
        if (_skins != null)
        {
            picture.sprite = _skins.GetSprite(_objectId, level);
            picture.color = _skins.GetColor(_objectId);
        }
    }

    // Button OnClick
    public void Click() => Clicked?.Invoke();
}
