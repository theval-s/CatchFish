using System;
using VContainer.Unity;

/// <summary>
/// Lists the school's classes, hires tutors and recruits students. While a class works, fetches the
/// profile once a minute so the laundered rubles and reputation keep moving.
/// </summary>
public class SchoolPresenter : IStartable, ITickable, IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private readonly SchoolView _view;
    private readonly SchoolService _school;
    private readonly ResourceService _resources;
    private readonly ConfigService _config;
    private readonly ServerTimeService _time;
    private DateTime _nextRefresh;

    public SchoolPresenter(SchoolView view, SchoolService school, ResourceService resources, ConfigService config,
        ServerTimeService time)
    {
        _view = view;
        _school = school;
        _resources = resources;
        _config = config;
        _time = time;
    }

    public void Start()
    {
        _view.Opened += OnOpened;
        _school.Changed += Refresh;
        _resources.Changed += Refresh;
        _nextRefresh = _time.Now + RefreshInterval;
        Refresh();
    }

    public void Dispose()
    {
        _view.Opened -= OnOpened;
        _school.Changed -= Refresh;
        _resources.Changed -= Refresh;
    }

    public void Tick()
    {
        if (_school.IsWorking && _time.Now >= _nextRefresh)
        {
            FetchPayout();
        }
    }

    private void OnOpened() => FetchPayout();

    private void FetchPayout()
    {
        _nextRefresh = _time.Now + RefreshInterval;
        _ = _school.RefreshAsync();
    }

    // Three classes: the list is simply rebuilt.
    private void Refresh()
    {
        _view.ShowSummary(_school.IsWorking
            ? $"Laundering {_school.LaunderPerMinute} DR/min, reputation +{_school.RepPerMinute}/min"
            : "Hire a tutor to start laundering your dirty rubles");

        _view.Clear();
        foreach (var data in _school.Classes)
        {
            var state = _school.Get(data);
            var item = _view.AddItem();
            var info = state.HasTutor
                ? $"Students: {state.Students}/{data.MaxStudents}\n" +
                  $"Launders {data.LaunderAt(state.Students)} DR/min, reputation +{data.RepAt(state.Students)}/min"
                : $"No tutor yet. With one: {data.LaunderBase} DR/min, +{data.LaunderPerStudent} per student";
            item.Show(data.Title, info);
            item.ShowHire(state.HasTutor ? null : $"Hire tutor: {ResourceNames.Price(data.TutorCost, _config)}",
                _school.CanHire(data));
            item.ShowStudent(!state.HasTutor ? null
                    : _school.IsFull(data) ? "Class is full"
                    : $"Add student: {ResourceNames.Price(_school.NextStudentCost(data), _config)}",
                _school.CanAddStudent(data));
            item.HireClicked += () => _ = _school.TryHireAsync(data);
            item.StudentClicked += () => _ = _school.TryAddStudentAsync(data);
        }
    }
}
