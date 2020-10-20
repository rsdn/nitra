﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;

namespace Nitra.VisualStudio
{
  internal static class Constants
  {
    public const string OutliningTaggerKey               = "Nitra-OutliningTagger";
    public const string ServerKey                        = "Nitra-Server";
    public const string FileIdKey                        = "Nitra-FileId";
    public const string NitraEditorClassifierKey         = "Nitra-EditorClassifier";
    public const string InteractiveHighlightingTaggerKey = "Nitra-InteractiveHighlightingTagger";
    public const string BraceMatchingSecond              = "NitraBraceMatchingSecond";
    public const string CurrentSymbol                    = "NitraCurrentSymbol";
    public const string FileModelKey                     = "Nitra-FileModel";
    public const string TextViewModelKey                 = "Nitra-TextViewModel";
    public const string CompilerMessagesTaggerKey        = "Nitra-CompilerMessagesTagger";
    public const string DefenitionHighlighting           = "NitraDefenitionHighlighting";
    public const string ReferenceHighlighting            = "NitraReferenceHighlighting";
    public const string NitraQuickInfoSourceKey          = "Nitra-NitraQuickInfoSource";
    public const string NitraCompleteWord                = "NitraCompleteWord";
    public static Guid  SolutionFolderGuid               = new Guid(VsProjectTypes.UnloadedProjectTypeGuid);
  }
}
