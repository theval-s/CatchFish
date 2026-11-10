using System;
using System.Threading.Tasks;

/// <summary>
/// Everything the client asks the backend for. The backend owns the profile: every action returns
/// the updated <see cref="GameState"/>, or throws <see cref="ApiException"/> if it was refused.
/// <see cref="HttpApiClient"/> talks to the server in Server/, <see cref="LocalApiClient"/> runs the
/// same rules on the device; <see cref="RootScope"/> picks one.
/// </summary>
public interface IApiClient
{
    Task<GameConfig> LoadConfigAsync();

    /// <summary>Creates the profile for a player who has never played.</summary>
    Task<GameState> LoadProfileAsync();

    Task<DateTime> GetServerTimeAsync();

    Task<GameState> BuyGearAsync(string gearId);

    Task<GameState> CompleteQuestAsync(int questId);

    /// <summary>Sets <see cref="GameState.ActiveRun"/>.</summary>
    Task<GameState> StartRunAsync();

    Task<GameState> CompleteRunAsync(RunResult result);
}
