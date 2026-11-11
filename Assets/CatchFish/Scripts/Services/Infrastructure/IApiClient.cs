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

    Task<GameConfig> LoadConfigAsync();

    Task<GameState> LoadProfileAsync();

    Task<DateTime> GetServerTimeAsync();

    Task<GameState> BuyGearAsync(string gearId);

    Task<GameState> CompleteQuestAsync(int questId);

    /// <summary>Sets <see cref="GameState.ActiveRun"/>.</summary>
    Task<GameState> StartRunAsync();

    Task<GameState> CompleteRunAsync(RunResult result);

    Task<GameState> ClaimDailyRewardAsync();
}
