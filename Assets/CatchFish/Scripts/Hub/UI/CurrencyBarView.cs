using TMPro;
using UnityEngine;

/// <summary>Currency counters in the top-right corner of the hub.</summary>
public class CurrencyBarView : MonoBehaviour
{
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text launderedText;
    [SerializeField] private TMP_Text dirtyText;

    public void Show(int gems, int laundered, int dirty)
    {
        gemsText.text = NumberFormat.Short(gems);
        launderedText.text = NumberFormat.Short(laundered);
        dirtyText.text = NumberFormat.Short(dirty);
    }
}
