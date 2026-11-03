using UnityEngine;

/// <summary>The Quests tab: a list of quest items, or a note when there are none.</summary>
public class QuestsView : MonoBehaviour
{
    [SerializeField] private QuestItemView itemPrefab;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject emptyLabel;

    public void Clear()
    {
        foreach (Transform child in itemContainer)
        {
            // Destroy happens at the end of the frame; hide it now so the layout skips it.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public QuestItemView AddItem() => Instantiate(itemPrefab, itemContainer);

    public void SetEmpty(bool empty) => emptyLabel.SetActive(empty);
}
