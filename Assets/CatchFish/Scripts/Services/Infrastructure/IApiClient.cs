using System;
using System.Threading.Tasks;

/// <summary>
/// Everything the client asks the server in Server/ for (the API is in Server/README.md). The server
/// owns the profile: every action returns the updated <see cref="GameState"/>, or throws
/// <see cref="ApiException"/> if it was refused.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Signs in as this device's guest, creating one on the first launch. Throws
    /// <see cref="ApiException.AccountNotFound"/> if the server doesn't know the stored guest.
    /// </summary>
    Task SignInAsync();

    /// <summary>Forgets this device's guest and signs in as a new player.</summary>
    Task StartNewPlayerAsync();

    /// <summary>
    /// Starts linking a Google account to this player: open the returned url in a browser, then poll
    /// <see cref="FinishGoogleSignInAsync"/> with the same secret.
    /// </summary>
    Task<GoogleSignInStart> BeginGoogleSignInAsync(string loginSecret);

    /// <summary>
    /// Null while the player hasn't finished in the browser. Throws <see cref="ApiException"/> identity_taken
    /// if the Google account has another player, unless <paramref name="switchPlayer"/>: then this device
    /// signs in as that player from now on.
    /// </summary>
    Task<GoogleSignInResult> FinishGoogleSignInAsync(string loginId, string loginSecret, bool switchPlayer);

    Task<AccountInfo> GetAccountAsync();

    Task<GameConfig> LoadConfigAsync();

    Task<GameState> LoadProfileAsync();

    Task<DateTime> GetServerTimeAsync();

    Task<GameState> BuyGearAsync(string gearId);

    Task<GameState> CompleteQuestAsync(int questId);

    /// <summary>Sets <see cref="GameState.ActiveRun"/>.</summary>
    Task<GameState> StartRunAsync();

    Task<GameState> CompleteRunAsync(RunResult result);

    Task<GameState> ClaimDailyRewardAsync();

    Task<GameState> RefillEnergyAsync();

    Task<GameState> HireTutorAsync(string classId);

    Task<GameState> AddStudentAsync(string classId);

    /// <summary>With <paramref name="useGems"/>, gems pay what the laundered rubles don't cover.</summary>
    Task<GameState> UpgradeHomeAsync(string objectId, bool useGems);

    Task<GameState> LevelUpHomeAsync(string objectId);
}
