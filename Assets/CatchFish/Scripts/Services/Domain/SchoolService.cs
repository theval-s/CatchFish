using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The school: hire tutors and recruit students with what the fishing brings in. Classes launder
/// dirty rubles and earn reputation every minute; the server pays them out on every request.
/// </summary>
public class SchoolService
{
    private static readonly SchoolClassState Untouched = new SchoolClassState();

    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;

    public SchoolService(IApiClient api, ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _resources = resources;
    }

    public event Action Changed;

    public IReadOnlyList<SchoolClassData> Classes => _config.Config.School.Classes;

    public int LaunderPerMinute => Sum(data => data.LaunderAt(Get(data).Students));

    public int RepPerMinute => Sum(data => data.RepAt(Get(data).Students));

    /// <summary>A class with a tutor is working: dirty rubles turn into clean ones over time.</summary>
    public bool IsWorking => Sum(data => 1) > 0;

    public SchoolClassState Get(SchoolClassData data) =>
        _profile.State.School.Classes.TryGetValue(data.Id, out var state) ? state : Untouched;

    public bool IsFull(SchoolClassData data) => Get(data).Students >= data.MaxStudents;

    public Dictionary<string, int> NextStudentCost(SchoolClassData data) => data.StudentCostAt(Get(data).Students);

    public bool CanHire(SchoolClassData data) => !Get(data).HasTutor && _resources.Has(data.TutorCost);

    public bool CanAddStudent(SchoolClassData data) =>
        Get(data).HasTutor && !IsFull(data) && _resources.Has(NextStudentCost(data));

    public Task<bool> TryHireAsync(SchoolClassData data) =>
        TryApplyAsync(CanHire(data), () => _api.HireTutorAsync(data.Id));

    public Task<bool> TryAddStudentAsync(SchoolClassData data) =>
        TryApplyAsync(CanAddStudent(data), () => _api.AddStudentAsync(data.Id));

    /// <summary>Fetches what the classes have laundered since the last request.</summary>
    public async Task RefreshAsync()
    {
        if (await _profile.TryRefreshAsync())
        {
            Changed?.Invoke();
        }
    }

    private async Task<bool> TryApplyAsync(bool allowed, Func<Task<GameState>> request)
    {
        if (!allowed || !await _profile.TryApplyAsync(request()))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    // Over the classes that have their tutor.
    private int Sum(Func<SchoolClassData, int> value)
    {
        var total = 0;
        foreach (var data in Classes)
        {
            if (Get(data).HasTutor)
            {
                total += value(data);
            }
        }

        return total;
    }
}
