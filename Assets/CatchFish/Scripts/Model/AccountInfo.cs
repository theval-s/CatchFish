/// <summary>How the player signs in, from GET /account.</summary>
public class AccountInfo
{
    public long PlayerId { get; set; }

    /// <summary>The linked Google account's email, or null.</summary>
    public string Google { get; set; }
}
