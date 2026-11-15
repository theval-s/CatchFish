/// <summary>A Google sign-in the server has started: the page to open, and the id to collect it with.</summary>
public class GoogleSignInStart
{
    public string LoginId { get; set; }
    public string Url { get; set; }
}
