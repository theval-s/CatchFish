using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>Tells the story before the first run, every time the hub opens until that run is done.</summary>
public class IntroDialogPresenter : IStartable, IDisposable
{
    private readonly DialogView _view;
    private readonly FishingService _fishing;
    private readonly ConfigService _config;
    private int _line;

    public IntroDialogPresenter(DialogView view, FishingService fishing, ConfigService config)
    {
        _view = view;
        _fishing = fishing;
        _config = config;
    }

    private IReadOnlyList<DialogLine> Lines => _config.Config.IntroDialog;

    public void Start()
    {
        _view.NextClicked += OnNext;
        _view.Hide();
        if (_fishing.IsFirstRun && Lines.Count > 0)
        {
            _line = 0;
            ShowLine();
        }
    }

    public void Dispose() => _view.NextClicked -= OnNext;

    private void OnNext()
    {
        _line++;
        if (_line < Lines.Count)
        {
            ShowLine();
        }
        else
        {
            _view.Hide();
        }
    }

    private void ShowLine() => _view.Show(Lines[_line].Speaker, Lines[_line].Text, _line == Lines.Count - 1);
}
