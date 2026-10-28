using System;
using System.Threading.Tasks;

/// <summary>
/// Everything the client asks the backend for. <see cref="LocalApiClient"/> fakes it on the device
/// until the real server exists; switching is one line in <see cref="RootScope"/>.
/// </summary>
public interface IApiClient
{
    Task<GameConfig> LoadConfigAsync();

    /// <summary>Returns null for a player who has never played.</summary>
    Task<GameState> LoadProfileAsync();

    Task SaveProfileAsync(GameState state);

    Task<DateTime> GetServerTimeAsync();
}
