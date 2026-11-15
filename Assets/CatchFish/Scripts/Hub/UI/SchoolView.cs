using System;
using TMPro;
using UnityEngine;

/// <summary>The School tab: what the classes earn per minute, and the list of classes.</summary>
public class SchoolView : MonoBehaviour
{
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private SchoolClassItemView itemPrefab;
    [SerializeField] private Transform itemContainer;

    /// <summary>Raised when the tab is shown.</summary>
    public event Action Opened;

    private void OnEnable() => Opened?.Invoke();

    public void ShowSummary(string text) => summaryText.text = text;

    public void Clear()
    {
        foreach (Transform child in itemContainer)
        {
            // Destroy happens at the end of the frame; hide it now so the layout skips it.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public SchoolClassItemView AddItem() => Instantiate(itemPrefab, itemContainer);
}
