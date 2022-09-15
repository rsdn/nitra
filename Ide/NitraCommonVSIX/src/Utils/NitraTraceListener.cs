using Nitra.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitra.VisualStudio.Utils
{
  class NitraTraceListener : TraceListener
  {
    public NitraTraceListener()
    {
    }

    public override bool IsThreadSafe => true;

    public override void Write(string message)
    {
      Log.WriteLine(message, ConsoleColor.Gray);
    }

    public override void WriteLine(string message)
    {
      Log.WriteLine(message, ConsoleColor.Gray);
    }

    public override void Flush()
    {
      Log.Flush();
    }

    public override void Fail(string message)
    {
      Fail(message, detailMessage: null);
    }

    public override void Fail(string message, string detailMessage)
    {
      var stackTrace = new System.Diagnostics.StackTrace(skipFrames: 4, fNeedFileInfo: true);
      if (string.IsNullOrWhiteSpace(message))
        Log.WriteLine("Assert failed!", ConsoleColor.Red);
      else
      {
        Log.WriteLine("Assert failed: ", ConsoleColor.Red);
        Log.WriteLine(message, ConsoleColor.Gray);
      }
      Log.WriteLine(stackTrace.ToString(), ConsoleColor.DarkRed);
    }

    private static ConsoleColor DeduceColor(string message)
    {
      return IsFailMessage(message) ? ConsoleColor.Red : ConsoleColor.Gray;
    }

    private static bool IsFailMessage(string message)
    {
      return message.StartsWith("Fail:", StringComparison.Ordinal);
    }
  }
}
