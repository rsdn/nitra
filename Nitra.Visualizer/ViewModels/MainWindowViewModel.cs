using Nitra.ClientServer.Messages;
using Nitra.ViewModels;
using Nitra.Visualizer.Properties;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Nitra.ClientServer.Client;

namespace Nitra.Visualizer.ViewModels
{
  public class MainWindowViewModel : ReactiveObject
  {
    [Reactive] public WorkspaceVm Workspace       { get; set; }
    [Reactive] public SuiteVm     CurrentSuite    { get; set; }
    [Reactive] public ProjectVm   CurrentProject  { get; set; }
    [Reactive] public FileVm      CurrentFile     { get; set; }
    [Reactive] public SolutionVm  CurrentSolution { get; set; }
    [Reactive] public Settings    Settings        { get; private set; }
    [Reactive] public string      StatusText      { get; private set; }

    public NitraTextEditorViewModel Editor { get; private set; }

    public IReactiveCommand<object> FindSymbolDefinitions { get; private set; }
    public IReactiveCommand<object> FindSymbolReferences  { get; private set; }
    public IReactiveCommand<object> NavigateBackward      { get; private set; }
    public IReactiveCommand<object> NavigateForkward      { get; private set; }

    readonly List<(FileVm file, NSpan span)> _history = new List<(FileVm, NSpan)>();
    int _historyIndex = -1;

    public MainWindowViewModel()
    {
      Editor = new NitraTextEditorViewModel(this);
      Settings = Settings.Default;

      var canFindSymbolDefinitions = this.WhenAny(v => v.CurrentSuite, v => v.CurrentFile,
                                                  (suite, test) => suite != null && test != null);

      FindSymbolDefinitions = ReactiveCommand.Create(canFindSymbolDefinitions);
      FindSymbolDefinitions.ThrownExceptions.Subscribe(e =>
        StatusText = "GOTO definition failed!");
      FindSymbolDefinitions.Subscribe(OnFindSymbolDefinitions);

      NavigateBackward = ReactiveCommand.Create(canFindSymbolDefinitions);
      NavigateBackward.ThrownExceptions.Subscribe(e =>
        StatusText = "Navigate Backward!");
      NavigateBackward.Subscribe(OnNavigateBackward);

      NavigateForkward = ReactiveCommand.Create(canFindSymbolDefinitions);
      NavigateForkward.ThrownExceptions.Subscribe(e =>
        StatusText = "Navigate forkward!");
      NavigateForkward.Subscribe(OnNavigateForkward);

      FindSymbolReferences = ReactiveCommand.Create(canFindSymbolDefinitions);
      FindSymbolReferences.ThrownExceptions.Subscribe(e =>
        StatusText = "Find all references definition failed!");
      FindSymbolReferences.Subscribe(OnFindSymbolReferences);

      Changing.Where(c => c.PropertyName == "Workspace")
        .Subscribe(_ => { if (Workspace != null) Workspace.Dispose(); });
    }

    private void OnFindSymbolReferences(object _)
    {
      var client = CurrentSuite.Client;
      var pos = Editor.CaretOffset;
      client.Send(new ClientMessage.FindSymbolReferences(CurrentProject.Id, CurrentFile.Id, new VersionedPos(pos, CurrentFile.Version)));
      var msg = client.Receive<ServerMessage.FindSymbolReferences>();

      if (msg.symbols.Length == 0)
      {
        StatusText = "No symbols found!";
        return;
      }

      var items = new List<PopupItemViewModel>();

      foreach (var s in msg.symbols)
      {
        int symbolId = s.SymbolId;

        foreach (var definition in s.Definitions)
        {
          var f = definition.Location.File;
          var file = CurrentSolution.GetFile(f.FileId);
          if (file.Version != f.FileVersion)
            continue;

          items.Add(new PopupItemViewModel(symbolId, file, definition.Location.Span, true, Editor.IntelliSensePopup));
        }

        foreach (var fileEntries in s.References)
        {
          var f = fileEntries.File;
          var file = CurrentSolution.GetFile(f.FileId);
          if (file.Version != f.FileVersion)
            continue;

          foreach (var range in fileEntries.Ranges)
            items.Add(new PopupItemViewModel(symbolId, file, range.Span, false, Editor.IntelliSensePopup));
        }
      }

      InitGotoList(items);

      Editor.IntelliSensePopup.IsVisible = true;
    }

    private void OnFindSymbolDefinitions(object _)
    {
      var client = CurrentSuite.Client;
      var pos = Editor.CaretOffset;

      client.Send(new ClientMessage.FindSymbolDefinitions(CurrentProject.Id, CurrentFile.Id, new VersionedPos(pos, CurrentFile.Version)));

      var msg = client.Receive<ServerMessage.FindSymbolDefinitions>();

      var currentLoc = (CurrentFile, new NSpan(Editor.CaretOffset, Editor.CaretOffset));
      if (_historyIndex >= 0 && _historyIndex < _history.Count)
        _history.RemoveRange(_historyIndex, _history.Count - _historyIndex);
      _history.Add(currentLoc);
      _historyIndex = _history.Count;

      if (msg.definitions.Length == 0 || msg.definitions.Length == 1 && msg.definitions[0].Location.File.FileId == -1)
        StatusText = "No symbols found!";
      else if (msg.definitions.Length == 1)
        Editor.SelectText(msg.definitions[0].Location);
      else
      {
        var items = new List<PopupItemViewModel>();
        Editor.IntelliSensePopup.Items.Clear();

        foreach (var definition in msg.definitions.OrderBy(d => d.Location.Span.StartPos))
        {
          int symbolId = definition.SymbolId;
          var loc      = definition.Location;
          var f        = loc.File;
          var file     = CurrentSolution.GetFile(f.FileId);

          if (file.Version != f.FileVersion)
            continue;

          items.Add(new PopupItemViewModel(symbolId, file, loc.Span, true, Editor.IntelliSensePopup));
        }

        InitGotoList(items);

        Editor.IntelliSensePopup.IsVisible = true;
      }
    }

    int BackwardIndex => _historyIndex - 1;
    int ForkwardIndex => _historyIndex;

    private void OnNavigateBackward(object _)
    {
      if (BackwardIndex < 0 || BackwardIndex >= _history.Count)
        return;

      var (file, span) = _history[BackwardIndex];
      Editor.SelectText(file, span);

      if (BackwardIndex > 0)
        _historyIndex--;
    }

    private void OnNavigateForkward(object _)
    {
      //Debug.Assert(ForkwardIndex >= 0 && ForkwardIndex <= _history.Count);

      if (ForkwardIndex < 0 || ForkwardIndex >= _history.Count)
        return;


      var (file, span) = _history[ForkwardIndex];
      Editor.SelectText(file, span);

      _historyIndex++;
    }

    private void InitGotoList(IEnumerable<PopupItemViewModel> items)
    {
      // We can use CreateDerivedCollection for list to be always sorted
      // Don't know if it is actually needed
      var models = items.OrderBy(i => i.File.Name).ThenBy(i => i.Span.StartPos);
      var list = Editor.IntelliSensePopup.Items;

      list.Clear();
      list.AddRange(models);

      // Need to call Reset manually, since multiple item notification is not supported by WPF
      // see this thread for answers: https://github.com/reactiveui/ReactiveUI/issues/363
      list.Reset();
    }
  }
}
