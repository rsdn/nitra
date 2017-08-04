using System;
using System.Collections.Generic;
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

    public NitraCompletionSet(ITrackingSpan applicableTo, ICompletionSession session, ITextSnapshot snapshot)
      : base("NitraWordCompletion", "Nitra word completion", applicableTo, new List<Completion>(24), new List<Completion>())
    {
      _session = session;
      _snapshot = snapshot;
    }

    public override void Recalculate()
    {
      var msg = (AsyncServerMessage.CompleteWord)_session.Properties[Constants.NitraCompleteWord];

      if (msg == null)
        return;


      var snapshot = _snapshot;
      var version = msg.Version;

      if (msg.Version != snapshot.Version.Convert())// && snapshot.TextBuffer.CurrentSnapshot is ITextSnapshot currentSnapshot && currentSnapshot.Version != snapshot.Version)
      {
        var currentSnapshot = snapshot.TextBuffer.CurrentSnapshot;
        var currentVersion = currentSnapshot.Version.Convert();
        if (currentVersion == version)
        {
          var span         = msg.replacementSpan;
          var applicableTo = currentSnapshot.CreateTrackingSpan(new Span(span.StartPos, span.Length), SpanTrackingMode.EdgeInclusive);
          this.ApplicableTo = applicableTo;
        }
      }

      //var triggerPoint = session.GetTriggerPoint(_textBuffer);
      //var snapshot = _textBuffer.CurrentSnapshot;
      //var version = snapshot.Version.Convert();
      //
      //if (msg.Version != version)
      //{
      //  return;
      //}
      this.WritableCompletions.Clear();
      FillCompletionList(msg, this.WritableCompletions);
      base.Recalculate();
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
