﻿using N2.Internal;

using Nemerle.Collections;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RecoveryStack = Nemerle.Core.list<N2.Internal.RecoveryStackFrame>.Cons;

namespace N2.Visualizer
{
  class ErrorException : Exception
  {
    public ErrorException(RecoveryResult recovery)
    {
      Recovery = recovery;
    }
    public RecoveryResult Recovery { get; private set; }
  }

  static class Utils
  {
    public static RecoveryStack Push(this RecoveryStack stack, RecoveryStackFrame elem)
    {
      return new RecoveryStack(elem, stack);
    }

    public static int Inc<T>(this Dictionary<T, int> heshtable, T key)
    {
      int value;
      heshtable.TryGetValue(key, out value);
      heshtable[key] = value;
      return value;
    }
  }

  class Recovery
  {
    RecoveryResult       _bestResult;
    int                  _parseCount;
    int                  _recCount;
    int                  _bestResultsCount;
    int                  _nestedLevel;
    Dictionary<int, int> _allacetionsInfo = new Dictionary<int, int>();
    Dictionary<object, int> _visited = new Dictionary<object, int>();
    Dictionary<string, int> _parsedRules = new Dictionary<string, int>();

    void Reset()
    {
      _bestResult = null;
      _parseCount = 0;
      _recCount = 0;
      _bestResultsCount = 0;
      _nestedLevel = 0;
      _allacetionsInfo.Clear();
      _visited = new Dictionary<object, int>();
      _parsedRules = new Dictionary<string, int>();
    }

    public RecoveryResult Strategy(int startTextPos, Parser parser)
    {
      Reset();
      var timer = System.Diagnostics.Stopwatch.StartNew();
      var recoveryStack = parser.RecoveryStack.NToList();
      var curTextPos    = startTextPos;
      var text          = parser.Text;

      parser.ParsingMode = ParsingMode.Parsing;
        
      do
      {
        for (var stack = recoveryStack as RecoveryStack; stack != null; stack = stack.Tail as RecoveryStack)
          ProcessStackFrame(startTextPos, ref parser, stack, curTextPos, text, 0);
        curTextPos++;
      }
      while (curTextPos - startTextPos < 800 && /*_bestResult == null && _bestResult == null && (res.Count == 0 || curTextPos - startTextPos < 10) &&*/ curTextPos <= text.Length);

      timer.Stop();
      var ex = new ErrorException(_bestResult);
      Reset();
      throw ex;
      //return _bestResult;
    }

    private void ProcessStackFrame(
      int startTextPos, 
      ref Parser parser, 
      RecoveryStack recoveryStack, 
      int curTextPos, 
      string text,
      int subruleLevel)
    {
      var stackFrame = recoveryStack.Head;
      var ruleParser = stackFrame.RuleParser;
      var lastState  = stackFrame.RuleParser.StatesCount - 1;

      var key = Tuple.Create(curTextPos, ruleParser);
      int startState;
      if (_visited.TryGetValue(key, out startState))
      {
        if (startState <= stackFrame.State)
          return;

        lastState = startState;
      }
      _visited[key] = stackFrame.State;

      for (var state = stackFrame.State; state <= lastState; state++)
      {
        parser.MaxTextPos = startTextPos;
        _parseCount++;
        var startAllocated = parser.allocated;
        int pos;

        var cnt = _parsedRules.Inc(ruleParser.RuleName);

        pos = ruleParser.TryParse(stackFrame.AstPtr, curTextPos, text, ref parser, state);

        var allocated = parser.allocated - startAllocated;
        int count = 0;
        _allacetionsInfo.TryGetValue(allocated, out count);
        count++;
        _allacetionsInfo[allocated] = count;

        if (pos > curTextPos || pos == text.Length)
        {
          var pos2 = ContinueParse(pos, recoveryStack, ref parser, text);
          AddResult(curTextPos,              pos2, state, recoveryStack, text, startTextPos);
        }
        else if (pos == curTextPos && state == lastState)
        {
          var pos2 = ContinueParse(pos, recoveryStack, ref parser, text);
          AddResult(curTextPos, pos2, state, recoveryStack, text, startTextPos);
        }
        else if (parser.MaxTextPos > curTextPos)
          AddResult(curTextPos, parser.MaxTextPos, state, recoveryStack, text, startTextPos);
        else
        {
          if (subruleLevel <= 0 && curTextPos == startTextPos)
          {
            if (_nestedLevel > 20) // ловим зацикленную рекурсию для целей отладки
              continue;
            _nestedLevel++;

            var parsers = ruleParser.GetParsersForState(state);
            foreach (var subRuleParser in parsers)
            {
              var old = recoveryStack;
              recoveryStack = recoveryStack.Push(new RecoveryStackFrame(subRuleParser, 0, stackFrame.AstPtr, 0, false));
              _recCount++;
              ProcessStackFrame(startTextPos, ref parser, recoveryStack, curTextPos, text, subruleLevel + 1);
              recoveryStack = old; // remove top element
            }

            _nestedLevel--;
          }
        }
      }
    }

    void AddResult(int startPos, int endPos, int startState, RecoveryStack stack, string text, int failPos)
    {
      _bestResultsCount++;

      int stackLength = 0;

      if (_bestResult == null)                   goto good;
      var skipedCount = startPos - failPos;

      if (skipedCount < _bestResult.SkipedCount) goto good;
      if (skipedCount > _bestResult.SkipedCount) return;

      if (endPos     > _bestResult.EndPos)       goto good;
      if (endPos     < _bestResult.EndPos)       return;

      if (startPos   < _bestResult.StartPos)     goto good;
      if (startPos   > _bestResult.StartPos)     return;

      stackLength = stack.Length;
      var bestResultStackLevel = this._bestResult.StackLevel;

      if (stackLength > bestResultStackLevel)    goto good;
      if (stackLength < bestResultStackLevel)    return;
      if (startState < _bestResult.StartState)   goto good;
      if (startState == _bestResult.StartState)  goto good2;
      return;
    good:
      _bestResult = new RecoveryResult(startPos, endPos, startState, stackLength, stack, text, failPos);
      return;
    good2:
      return;
    }

    int ContinueParse(int startTextPos, RecoveryStack recoveryStack, ref Parser parser, string text)
    {
      var tail = recoveryStack.Tail as RecoveryStack;

      if (tail == null)
        return startTextPos;

      var recoveryInfo = tail.Head;
      var nextState = recoveryInfo.IsList ? recoveryInfo.State : recoveryInfo.State + 1;
      var pos3 =
        nextState >= recoveryInfo.RuleParser.StatesCount
          ? startTextPos
          : recoveryInfo.RuleParser.TryParse(recoveryInfo.AstPtr, startTextPos, text, ref parser, nextState);

      if (pos3 >= 0)
        return ContinueParse(pos3, tail, ref parser, text);
      else
        return Math.Max(parser.MaxTextPos, startTextPos);
    }
  }
}
