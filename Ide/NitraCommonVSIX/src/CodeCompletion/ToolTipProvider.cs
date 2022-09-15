using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Nitra.VisualStudio.CodeCompletion
{
  [Export(typeof(IUIElementProvider<VSCompletion, ICompletionSession>))]
  [Name("NytraToolTipProvider")]
  [ContentType("nitra")]
  internal class ToolTipProvider : IUIElementProvider<VSCompletion, ICompletionSession>
  {
    public UIElement GetUIElement(VSCompletion itemToRender, ICompletionSession context, UIElementType elementType)
    {
      return null;
    }
  }
}
