/// <summary>A finished Google sign-in.</summary>
public class GoogleSignInResult
{
    public string Email { get; set; }

    /// <summary>The device now plays as the Google account's player, not the guest it was.</summary>
    public bool SwitchedPlayer { get; set; }
}
