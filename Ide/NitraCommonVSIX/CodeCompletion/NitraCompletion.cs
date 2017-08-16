using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Nitra.VisualStudio.CodeCompletion
{
  class NitraCompletion : Completion //, ITextFormattable
  {
    public NitraCompletion(string displayText, string insertionText, string description, ImageSource iconSource, string iconAutomationText)
      : base(displayText, insertionText, description, iconSource, iconAutomationText)
    {
    }

    //public TextRunProperties GetHighlightedTextRunProperties(TextRunProperties defaultHighlightedTextRunProperties)
    //{
    //  return TextFormattingRunProperties.CreateTextFormattingRunProperties(defaultHighlightedTextRunProperties.Typeface, defaultHighlightedTextRunProperties.FontHintingEmSize, Colors.Red);
    //}
    //
    //public TextRunProperties GetTextRunProperties(TextRunProperties defaultTextRunProperties)
    //{
    //  return TextFormattingRunProperties.CreateTextFormattingRunProperties(defaultTextRunProperties.Typeface, defaultTextRunProperties.FontHintingEmSize, Colors.Blue);
    //}
  }
}
