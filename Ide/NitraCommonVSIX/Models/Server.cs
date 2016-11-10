﻿using Nitra.ClientServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NitraCommonIde;
using System.Diagnostics;
using Nitra.ClientServer.Messages;

using Ide = NitraCommonIde;
using M = Nitra.ClientServer.Messages;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using System.Collections.Immutable;
using Nitra.VisualStudio.Highlighting;
using Nitra.VisualStudio.BraceMatching;
using Nitra.VisualStudio.Models;
using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.Shell.Interop;

namespace Nitra.VisualStudio
{
  class Server : IDisposable
  {
    Ide.Config _config;
    public IServiceProvider ServiceProvider { get; }

    public NitraClient Client { get; private set; }

    public Server(StringManager stringManager, Ide.Config config, IServiceProvider serviceProvider)
    {
      Contract.Requires(ServiceProvider != null);

      ServiceProvider = serviceProvider;

      var client = new NitraClient(stringManager);
      client.Send(new ClientMessage.CheckVersion(M.Constants.AssemblyVersionGuid));
      var responseMap = client.ResponseMap;
      responseMap[-1] = Response;
      _config = config;
      Client = client;
    }

    private ImmutableArray<SpanClassInfo> _spanClassInfos = ImmutableArray<SpanClassInfo>.Empty;
    public ImmutableArray<SpanClassInfo> SpanClassInfos { get { return _spanClassInfos; } }


    private static M.Config ConvertConfig(Ide.Config config)
    {
      var ps = config.ProjectSupport;
      var projectSupport = new M.ProjectSupport(ps.Caption, ps.TypeFullName, ps.Path);
      var languages = config.Languages.Select(x => new M.LanguageInfo(x.Name, x.Path, new M.DynamicExtensionInfo[0])).ToArray();
      var msgConfig = new M.Config(projectSupport, languages, new string[0]);
      return msgConfig;
    }

    public void Dispose()
    {
      Client?.Dispose();
    }

    internal void SolutionStartLoading(SolutionId id, string solutionPath)
    {
      Client.Send(new ClientMessage.SolutionStartLoading(id, solutionPath));
    }

    internal void CaretPositionChanged(FileId fileId, int pos, FileVersion version)
    {
      Client.Send(new ClientMessage.SetCaretPos(fileId, version, pos));
    }

    internal void SolutionLoaded(SolutionId solutionId)
    {
      Client.Send(new ClientMessage.SolutionLoaded(solutionId));
    }

    internal void ProjectStartLoading(ProjectId id, string projectPath)
    {
      var config = ConvertConfig(_config);
      Client.Send(new ClientMessage.ProjectStartLoading(id, projectPath, config));
    }

    internal void ProjectLoaded(ProjectId id)
    {
      Client.Send(new ClientMessage.ProjectLoaded(id));
    }

    internal void ReferenceAdded(ProjectId projectId, string referencePath)
    {
      Client.Send(new ClientMessage.ReferenceLoaded(projectId, "File:" + referencePath));
    }

    internal void BeforeCloseProject(ProjectId id)
    {
      Client.Send(new ClientMessage.ProjectUnloaded(id));
    }

    internal void FileAdded(ProjectId projectId, string path, FileId id, FileVersion version)
    {
      Client.Send(new ClientMessage.FileLoaded(projectId, path, id, version));
    }

    internal void FileUnloaded(FileId id)
    {
      Client.Send(new ClientMessage.FileUnloaded(id));
    }

    internal void ViewActivated(IWpfTextView wpfTextView, FileId id, IVsHierarchy hierarchy, string fullPath)
    {
      var textBuffer = wpfTextView.TextBuffer;

      if (!textBuffer.Properties.ContainsProperty(Constants.ServerKey))
        textBuffer.Properties.AddProperty(Constants.ServerKey, this);

      FileModel fileModel = VsUtils.GetOrCreateFileModel(wpfTextView, id, this, hierarchy, fullPath);
      TextViewModel textViewModel = VsUtils.GetOrCreateTextViewModel(wpfTextView, fileModel);

      fileModel.ViewActivated(textViewModel);
    }

    internal void ViewDeactivated(IWpfTextView wpfTextView, FileId id)
    {
      var fileModel = wpfTextView.TextBuffer.Properties.GetProperty<FileModel>(Constants.FileModelKey);
      fileModel.Remove(wpfTextView);
    }

    void Response(AsyncServerMessage msg)
    {
      AsyncServerMessage.LanguageLoaded languageInfo;

      if ((languageInfo = msg as AsyncServerMessage.LanguageLoaded) != null)
      {
        var spanClassInfos = languageInfo.spanClassInfos;
        if (_spanClassInfos.IsDefaultOrEmpty)
          _spanClassInfos = spanClassInfos;
        else if (!spanClassInfos.IsDefaultOrEmpty)
        {
          var bilder = ImmutableArray.CreateBuilder<SpanClassInfo>(_spanClassInfos.Length + spanClassInfos.Length);
          bilder.AddRange(_spanClassInfos);
          bilder.AddRange(spanClassInfos);
          _spanClassInfos = bilder.MoveToImmutable();
        }
      }
    }
  }
}