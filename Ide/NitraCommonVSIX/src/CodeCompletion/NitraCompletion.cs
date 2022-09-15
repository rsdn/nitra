using Microsoft.VisualStudio.Language.Intellisense;
using System.Windows.Media;

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
