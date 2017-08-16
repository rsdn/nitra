using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using static System.String;
using Nitra.ClientServer.Messages;

namespace Nitra.VisualStudio.CodeCompletion
{
  class NitraCompletionSet : CompletionSet
  {
    ICompletionSession _session;
    ITextSnapshot      _snapshot;
    string             _filterText;

    public NitraCompletionSet(ITrackingSpan applicableTo, ICompletionSession session, ITextSnapshot snapshot)
      : base("NitraWordCompletion", "Nitra word completion", applicableTo, new List<Completion>(24), new List<Completion>())
    {
      _session = session;
      _snapshot = snapshot;
      _filterText = "";
    }

    public override void Recalculate()
    {
      var msg = (AsyncServerMessage.CompleteWord)_session.Properties[Constants.NitraCompleteWord];

      if (msg == null)
        return;


      var snapshot = _snapshot;
      var version = msg.Version;
      var currentSnapshot = snapshot.TextBuffer.CurrentSnapshot;

      if (msg.Version != snapshot.Version.Convert())// && snapshot.TextBuffer.CurrentSnapshot is ITextSnapshot currentSnapshot && currentSnapshot.Version != snapshot.Version)
      {
        var currentVersion = currentSnapshot.Version.Convert();
        if (currentVersion == version)
        {
          var span         = msg.replacementSpan;
          var applicableTo = currentSnapshot.CreateTrackingSpan(new Span(span.StartPos, span.Length), SpanTrackingMode.EdgeInclusive);
          this.ApplicableTo = applicableTo;
        }
      }

      _filterText = this.ApplicableTo.GetText(currentSnapshot);

      this.WritableCompletions.Clear();
      FillCompletionList(msg, this.WritableCompletions);
      base.Recalculate();
    }

    public override void SelectBestMatch()
    {
      //this.SelectionStatus = new CompletionSelectionStatus();
      base.SelectBestMatch();
    }

    public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
    {
      var spans = StringPatternMatching.MatchPatternSpans(displayText, _filterText);
      return spans.Select(s => new Span(s.StartPos, s.Length)).ToImmutableArray();
    }

    public override void Filter()
    {
      var filteredCompletions = (FilteredObservableCollection<Completion>)this.Completions;
      var pattern = ApplicableTo.GetText(_snapshot);
      filteredCompletions.Filter(c => StringPatternMatching.MatchPattern(c.InsertionText, pattern));
    }

    static void FillCompletionList(AsyncServerMessage.CompleteWord msg, BulkObservableCollection<Completion> completions)
    {
      foreach (var elem in msg.completionList)
      {
        switch (elem)
        {
          case CompletionElem.Literal literal:
            completions.Add(new Completion(literal.text, literal.text, "literal", null, null));
            break;
          case CompletionElem.Symbol symbol:
            completions.Add(new Completion(symbol.name, symbol.name, symbol.description, null, null));
            break;
        }
      }
    }
  }
}
