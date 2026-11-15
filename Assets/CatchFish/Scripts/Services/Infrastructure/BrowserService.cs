using UnityEngine;

/// <summary>Opens pages in the system browser (Google's sign-in page refuses embedded web views).</summary>
public class BrowserService
{
    public void Open(string url) => Application.OpenURL(url);
}
