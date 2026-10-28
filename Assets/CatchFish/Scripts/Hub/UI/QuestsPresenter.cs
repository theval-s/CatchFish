using System;
using System.Text;
using VContainer.Unity;

/// <summary>Lists the open quests with the player's progress and completes them.</summary>
public class QuestsPresenter : IStartable, IDisposable
{
    private readonly QuestsView _view;
    private readonly QuestService _quests;
    private readonly ResourceService _resources;
    private readonly ConfigService _config;

    public QuestsPresenter(QuestsView view, QuestService quests, ResourceService resources, ConfigService config)
    {
        _view = view;
        _quests = quests;
        _resources = resources;
        _config = config;
    }

    public void Start()
    {
        _quests.Changed += Refresh;
        Refresh();
    }

    public void Dispose() => _quests.Changed -= Refresh;

    // There are only a few quests, so the list is simply rebuilt.
    private void Refresh()
    {
        _view.Clear();

        var quests = _quests.ActiveQuests;
        _view.SetEmpty(quests.Count == 0);
        foreach (var quest in quests)
        {
            var item = _view.AddItem();
            item.Show(quest.Title, quest.Description, DescribeRequirements(quest), _quests.CanComplete(quest));
            item.CompleteClicked += () => _quests.TryComplete(quest);
        }
    }

    private string DescribeRequirements(QuestData quest)
    {
        var text = new StringBuilder();
        foreach (var pair in quest.Requirements)
        {
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append($"{ResourceName(pair.Key)}: {_resources.Get(pair.Key)} / {pair.Value}");
        }

        return text.ToString();
    }

    private string ResourceName(string id)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles: return "Dirty rubles";
            case ResourceIds.LaunderedRubles: return "Laundered rubles";
            case ResourceIds.Reputation: return "Reputation";
            case ResourceIds.Gems: return "Gems";
        }

        var fish = _config.GetFish(id);
        return fish != null ? fish.Name : id;
    }
}
