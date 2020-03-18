//------------------------------------------------------------------------------
// <copyright file="VSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;

using Nitra.ClientServer.Client;
using Nitra.ClientServer.Messages;
using Nitra.Logging;
using Nitra.VisualStudio.Utils;
using NitraCommonIde;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using VSLangProj;

using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;

namespace Nitra.VisualStudio
{
  /// <summary>
  /// This is the class that implements the package exposed by this assembly.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The minimum requirement for a class to be considered a valid package for Visual Studio
  /// is to implement the IVsPackage interface and register itself with the shell.
  /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
  /// to do it: it derives from the Package class that provides the implementation of the
  /// IVsPackage interface and uses the registration attributes defined in the framework to
  /// register itself and its components with the shell. These attributes tell the pkgdef creation
  /// utility what data to put into .pkgdef file.
  /// </para>
  /// <para>
  /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
  /// </para>
  /// </remarks>
  [ProvideAutoLoad(UIContextGuids80.NoSolution)]
  [Description("Nitra Package.")]
  [PackageRegistration(UseManagedResourcesOnly = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
  [Guid(NitraCommonVsPackage.PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  public sealed class NitraCommonVsPackage : Package
  {
    /// <summary>VSPackage GUID string.</summary>
    public const string PackageGuidString = "66c3f4cd-1547-458b-a321-83f0c448b4d3";
    public static SolutionId InvalidSolutionId = new SolutionId(-1);


    public static NitraCommonVsPackage Instance;

    readonly Dictionary<ProjectId, HashSet<string>> _referenceMap = new Dictionary<ProjectId, HashSet<string>>();
    readonly Dictionary<IVsHierarchy, HierarchyListener> _listenersMap = new Dictionary<IVsHierarchy, HierarchyListener>();
    readonly List<Project> _projects = new List<Project>();
    readonly List<ServerModel> _servers = new List<ServerModel>();
    readonly StringManager _stringManager = new StringManager();
    RunningDocTableEvents _runningDocTableEventse;
    ProjectItemsEvents _prjItemsEvents;
    EnvDTE.SolutionEvents _solutionEvents;
    uint _objectManagerCookie;
    Library _library;
    SolutionLoadingSate _backgroundLoading;
    SolutionId _currentSolutionId = InvalidSolutionId;

    internal List<ServerModel> Servers { get => _servers; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NitraCommonVsPackage"/> class.
    /// </summary>
    public NitraCommonVsPackage()
    {
      Log.Init("Nitra-VS-plug-in");
      // Inside this method you can place any initialization code that does not require
      // any Visual Studio service because at this point the package object is created but
      // not sited yet inside Visual Studio environment. The place to do all the other
      // initialization is the Initialize method.
      //AppDomain.CurrentDomain.FirstChanceException +=
      //(object source, FirstChanceExceptionEventArgs e) =>
      //{
      //  var ex = e.Exception;
      //
      //  switch (ex)
      //  {
      //    case FileLoadException _:
      //    case OperationCanceledException _:
      //      return;
      //  }
      //
      //  Log.Exception(ex);
      //};
    }

    public void SetFindResult(IVsSimpleObjectList2 findResults)
    {
      _library.OnFindAllReferencesDone(findResults);
    }

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    protected override void Initialize()
    {
      //var listener = new NitraTraceListener();
      //Trace.Listeners.Clear();
      //Trace.Listeners.Add(listener);

      ThreadHelper.ThrowIfNotOnUIThread();
      base.Initialize();
      Debug.Assert(Instance == null);
      Instance = this;

      DTE2 dte = (DTE2)GetService(typeof(DTE));
      Assumes.Present(dte);
      //Debug.Assert(false);
      // We must cache ProjectItemsEvents in a field to prevent free it by GC. Don't in-line the _prjItemsEvents!
      var events = (Events2)dte.Events;
      var x = events.SolutionItemsEvents;
      _solutionEvents = events.SolutionEvents;
      _solutionEvents.Opened += _solutionEvents_Opened;
      _solutionEvents.BeforeClosing += _solutionEvents_BeforeClosing;
      _solutionEvents.ProjectAdded += _solutionEvents_ProjectAdded;
      _solutionEvents.ProjectRemoved += _solutionEvents_ProjectRemoved;
      _solutionEvents.ProjectRenamed += _solutionEvents_ProjectRenamed;
      _solutionEvents.Renamed += _solutionEvents_Renamed;
      _prjItemsEvents = events.ProjectItemsEvents;
      _prjItemsEvents.ItemAdded += PrjItemsEvents_ItemAdded;
      _prjItemsEvents.ItemRemoved += PrjItemsEvents_ItemRemoved;
      _prjItemsEvents.ItemRenamed += PrjItemsEvents_ItemRenamed;

      _runningDocTableEventse = new RunningDocTableEvents();
      _runningDocTableEventse.DocumentSaved += _runningDocTableEventse_DocumentSaved;
      SubscibeToSolutionEvents();

      if (_objectManagerCookie == 0)
      {
        _library = new Library();
        var objManager = this.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;

        if (null != objManager)
          ErrorHandler.ThrowOnFailure(objManager.RegisterSimpleLibrary(_library, out _objectManagerCookie));
      }
    }

    private void _solutionEvents_Renamed(string OldName)
    {
      Log.Message($"tr: _solutionEvents_Renamed(OldName='{OldName}')");
    }

    private void _solutionEvents_ProjectRenamed(Project Project, string OldName)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      Log.Message($"tr: _solutionEvents_ProjectRenamed(Project='{Project.FullName}', OldName='{OldName}')");
    }

    private void _solutionEvents_ProjectRemoved(Project Project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      Log.Message($"tr: _solutionEvents_ProjectRemoved(Project='{Project.FullName}')");
    }

    private void _solutionEvents_ProjectAdded(Project Project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      Log.Message($"tr: _solutionEvents_ProjectAdded(Project='{Project.FullName}')");
    }

    private void _solutionEvents_BeforeClosing()
    {
      Log.Message($"tr: _solutionEvents_BeforeClosing()");
    }

    private void _solutionEvents_Opened()
    {
      Log.Message($"tr: _solutionEvents_Opened()");
    }

    private void _runningDocTableEventse_DocumentSaved(object sender, string path)
    {
      foreach (var server in _servers)
        server.FileSaved(path);
    }

    private void PrjItemsEvents_ItemAdded(ProjectItem projectItem)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      Log.Message($"tr: PrjItemsEvents_ItemAdded(projectItem={projectItem.Name})");

      if (_backgroundLoading != SolutionLoadingSate.Loaded)
        return;

      ThreadHelper.ThrowIfNotOnUIThread();

      string fullPath = projectItem.FileNames[1];
      if (fullPath == null)
        return;

      AddFile(projectItem, fullPath);
    }

    private void PrjItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var newFilePath = (string)projectItem.Properties.Item("FullPath").Value;
      var oldFilePath = Path.Combine(Path.GetDirectoryName(newFilePath), oldName);
      var project = projectItem.ContainingProject;
      var projectPath = project.FullName;
      var projectId = GetProjectId(project);
      var ext = Path.GetExtension(newFilePath);
      var newFileId = new FileId(_stringManager.GetId(newFilePath));
      var oldFileId = new FileId(_stringManager.GetId(oldFilePath));

      Log.Message($"tr: ItemRenamed(newFileId={newFileId} oldFileId={oldFileId} newFilePath='{newFilePath}' oldFilePath='{oldFilePath}' projectId={projectId} projectPath='{projectPath}')");

      foreach (var server in _servers)
        if (server.IsSupportedExtension(ext))
          server.FileRenamed(oldFileId, newFileId, newFilePath);

    }

    private void PrjItemsEvents_ItemRemoved(ProjectItem projectItem)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var filePath = (string)projectItem.Properties.Item("FullPath").Value;
      var project = projectItem.ContainingProject;
      var projectPath = project.FullName;
      var ext = Path.GetExtension(filePath);
      var id = new FileId(_stringManager.GetId(filePath));
      var projectId = GetProjectId(project);

      Log.Message($"tr: ItemRemoved(FileName='{filePath}' id={id} projectPath='{projectPath}')");

      foreach (var server in _servers)
        if (server.IsSupportedExtension(ext))
          server.FileUnloaded(projectId, id);

    }

    protected override void Dispose(bool disposing)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      try
      {
        foreach (var server in _servers)
          server.Dispose();

        UnsubscibeToSolutionEvents();
        _runningDocTableEventse?.Dispose();
        _runningDocTableEventse = null;

        var objManager = GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
        if (objManager != null)
          objManager.UnregisterLibrary(_objectManagerCookie);
      }
      finally
      {
        base.Dispose(disposing);
      }
    }

    #endregion

    // /////////////////////////////////////////////////////////////////////////////////////////////

    void QueryUnloadProject(object sender, CancelHierarchyEventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var hierarchy = e.Hierarchy;
      var project = hierarchy.GetProp<Project>(VSConstants.VSITEMID_ROOT, __VSHPROPID.VSHPROPID_ExtObject);
      Log.Message($"tr: QueryUnloadProject(FullName='{project.FullName}')");
    }

    void SolutionEvents_OnQueryCloseSolution(object sender, CancelEventArgs e)
    {
      Log.Message($"tr: QueryCloseSolution(Cancel='{e.Cancel}')");
    }

    void QueryCloseProject(object sender, QueryCloseProjectEventArgs e)
    {
      var hierarchy = e.Hierarchy;
      var project = hierarchy.GetProp<Project>(VSConstants.VSITEMID_ROOT, __VSHPROPID.VSHPROPID_ExtObject);
      Log.Message($"tr: QueryCloseProject(IsRemoving='{e.IsRemoving}', Cancel='{e.Cancel}', FullName='{project?.FullName}')");
    }

    void QueryChangeProjectParent(object sender, QueryChangeProjectParentEventArgs e)
    {
      Log.Message($"tr: QueryChangeProjectParent(Hierarchy='{e.Hierarchy}', NewParentHierarchy='{e.NewParentHierarchy}', Cancel='{e.Cancel}')");
    }

    void QueryBackgroundLoadProjectBatch(object sender, QueryLoadProjectBatchEventArgs e)
    {
      Log.Message($"tr: QueryBackgroundLoadProjectBatch(ShouldDelayLoadToNextIdle='{e.ShouldDelayLoadToNextIdle}')");
    }

    void BeforeUnloadProject(object sender, LoadProjectEventArgs e)
    {
      Log.Message($"tr: BeforeUnloadProject(RealHierarchy='{e.RealHierarchy}', StubHierarchy='{e.StubHierarchy}')");
    }

    void BeforeOpenSolution(object sender, BeforeOpenSolutionEventArgs e)
    {
      InitSolution(e.SolutionFilename);
    }

    void InitSolution(string solutionPath)
    {
      _backgroundLoading = SolutionLoadingSate.SynchronousLoading;

      InitServers();

      var id = new SolutionId(_stringManager.GetId(solutionPath));
      _currentSolutionId = id;

      Log.Message($"tr: BeforeOpenSolution(SolutionFilename='{solutionPath}' id={id})");

      foreach (var server in _servers)
        server.SolutionStartLoading(id, solutionPath);
    }

    private void InitServers()
    {
      if (_servers.Count > 0)
        return; // already initialized

      if (NitraCommonPackage.Configs.Count == 0)
      {
        Log.Message($"Error: Configs is empty!)");
      }

      var stringManager = _stringManager;

      foreach (var config in NitraCommonPackage.Configs)
      {
        var server = new ServerModel(stringManager, config, this);
        _servers.Add(server);
      }

      return;
    }

    void BeforeOpenProject(object sender, BeforeOpenProjectEventArgs e)
    {
      if (_backgroundLoading == SolutionLoadingSate.NotLoaded)
      {
        // This is a separate project which  saw opened without the Solution. We need init the fake solution.
        InitSolution("<no solution>");
      }
      Log.Message($"tr: BeforeOpenProject(Filename='{e.Filename}', Project='{e.Project}'  ProjectType='{e.ProjectType}')");
    }

    void BeforeOpeningChildren(object sender, HierarchyEventArgs e)
    {
      Log.Message($"tr: BeforeOpeningChildren(Hierarchy='{e.Hierarchy}')");
    }

    void BeforeLoadProjectBatch(object sender, LoadProjectBatchEventArgs e)
    {
      Log.Message($"tr: BeforeLoadProjectBatch(IsBackgroundIdleBatch='{e.IsBackgroundIdleBatch}')");
    }

    void BeforeClosingChildren(object sender, HierarchyEventArgs e)
    {
      Log.Message($"tr: BeforeClosingChildren(Hierarchy='{e.Hierarchy}')");
    }

    void BeforeCloseSolution(object sender, EventArgs e)
    {
      foreach (var server in _servers)
        server.Dispose();

      _servers.Clear();

      Log.Message($"tr: BeforeCloseSolution()");
    }

    void BeforeBackgroundSolutionLoadBegins(object sender, EventArgs e)
    {
      _backgroundLoading = SolutionLoadingSate.AsynchronousLoading;

      Log.Message($"tr: BeforeBackgroundSolutionLoadBegins()");
    }

    void AfterRenameProject(object sender, HierarchyEventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var hierarchy = e.Hierarchy;
      var project = hierarchy.GetProp<Project>(VSConstants.VSITEMID_ROOT, __VSHPROPID.VSHPROPID_ExtObject);
      Log.Message($"tr: AfterRenameProject(Hierarchy='{hierarchy}', FullName='{project.FullName}')");
    }

    void AfterOpenSolution(object sender, OpenSolutionEventArgs e)
    {
      var isTemporarySolution = _currentSolutionId == InvalidSolutionId;
      if (isTemporarySolution)
        _currentSolutionId = new SolutionId(0); // This is temporary solution for <MiscFiles>

      InitServers(); // need in case of open separate files (with no project)

      Debug.Assert(_backgroundLoading != SolutionLoadingSate.AsynchronousLoading);

      var path = _stringManager.GetPath(_currentSolutionId);
      Log.Message($"tr: AfterOpenSolution(IsNewSolution='{e.IsNewSolution}', Id='{_currentSolutionId}' Path='{path}')");

      foreach (var server in _servers)
        if (isTemporarySolution)
          server.SolutionStartLoading(_currentSolutionId, ""); // init "<MiscFiles>" solution

      foreach (var listener in _listenersMap.Values)
        listener.StartListening(true);

      // scan only currently loaded projects
      foreach (var project in _projects)
      {
        try
        {
          ScanReferences(project);
        }
        catch (Exception ex)
        {
          Log.Exception(ex);
        }
      }

      foreach (var project in _projects)
      {
        try
        {
          ScanFiles(project);
        }
        catch (Exception ex)
        {
          Log.Exception(ex);
        }
      }

      foreach (var project in _projects)
        foreach (var server in _servers)
          server.ProjectLoaded(GetProjectId(project));

      _backgroundLoading = SolutionLoadingSate.Loaded;
    }

    void ScanReferences(Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      Log.Message($"tr: ScanReferences(started) Project='{project.Name}'");

      try
      {
        if (project.Object is VSProject vsproject)
        {
          var projectId = GetProjectId(project);
          var references = new HashSet<string>();
          _referenceMap.Add(projectId, references);
          var exceptionCount = 0;

          foreach (Reference reference in vsproject.References)
          {
            try
            {
              var path = reference.Path;

              if (!string.IsNullOrEmpty(path))
                references.Add(path);

              if (reference.SourceProject == null)
              {
                if (string.IsNullOrEmpty(path))
                {
                  Log.Message($"tr:    Error: reference.Path=null reference.Name={reference.Name}");
                  continue;
                }

                foreach (var server in _servers)
                  server.ReferenceAdded(projectId, path);
                Log.Message($"tr:    Reference: Name={reference.Name} Path={path}");
              }
              else
              {
                if (string.IsNullOrEmpty(path))
                {
                  // This situation occurs when a referenced project is missing
                  continue;
                }

                var referencedProjectId = GetProjectId(reference.SourceProject);
                foreach (var server in _servers)
                  server.ProjectReferenceAdded(projectId, referencedProjectId, path);
                Log.Message($"tr:    Project reference: ProjectId={referencedProjectId} Project={reference.SourceProject.Name} ProjectPath={reference.SourceProject.FullName} DllPath={reference.Path}");
              }
            }
            catch (Exception ex)
            {
              exceptionCount++;
              Log.Exception(ex);
              if (exceptionCount == 5)
              {
                Log.Message($@"Project upload was interrupted due to exceeding the exception threshold ({exceptionCount}).", ConsoleColor.Yellow);
                break;
              }
            }
          }
        }
        else
          Log.Message("tr:    Error: project.Object=null");
      }
      catch (Exception ex)
      {
        Log.Exception(ex);
      }

      Log.Message("tr: ScanReferences(finished)");
    }

    void ScanFiles(Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      Log.Message($"tr: ScanFiles(started) Project='{project.Name}'");

      if (project.Object is VSProject vsproject)
      {
        var projectId = GetProjectId(project);

        ProjectItems projectItems = project.ProjectItems;

        ScanFiles(project, projectItems);
      }
      else
        Log.Message("tr:    Error: project.Object=null");

      Log.Message("tr: ScanFiles(finished)");
    }

    private void ScanFiles(Project project, ProjectItems projectItems)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      Log.Message($"tr:    ScanFiles(ProjectItem: Project={project.Name})");

      foreach (ProjectItem item in projectItems)
      {
        //var filePath = (string)item.Properties.Item("FullPath").Value;
        var filePath = item.FileNames[1];
        Log.Message($"tr:    ProjectItem: Name={item.Name} Project={project.Name} filePath={filePath} Kind={item.Kind}");

        try
        {
          if (Guid.TryParse(item.Kind, out var guid) && guid == VSConstants.GUID_ItemType_PhysicalFile)
            AddFile(item, filePath);
        }
        catch (Exception ex)
        {
          Log.Exception(ex);
        }

        if (item.ProjectItems?.Count > 0)
          ScanFiles(project, item.ProjectItems);

      }
      Log.Message($"tr:    ScanFiles finished!");
    }

    ProjectId GetProjectId(Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      return new ProjectId(_stringManager.GetId(project.FullName));
    }

    void AfterBackgroundSolutionLoadComplete(object sender, EventArgs e)
    {
      var path = _stringManager.GetPath(_currentSolutionId);
      Log.Message($"tr: AfterBackgroundSolutionLoadComplete(Id={_currentSolutionId} Path='{path}')");

      foreach (var server in _servers)
        server.SolutionLoaded(_currentSolutionId);

      _backgroundLoading = SolutionLoadingSate.Loaded;
    }

    void AfterOpenProject(object sender, OpenProjectEventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var hierarchy = e.Hierarchy;
      var project = hierarchy.GetProp<Project>(VSConstants.VSITEMID_ROOT, __VSHPROPID.VSHPROPID_ExtObject);

      if (project == null)
        return; // not supported project type

      if (project.Name == VsProjectTypes.NuGetSolutionSettingsFolder)
        return; // not supported project type

      var isMiscFiles = false;

      switch (project.Kind)
      {
        case VsProjectTypes.VsProjectItemKindSolutionItem:
          Debug.Assert(false, "Unexpected project kind: VsProjectItemKindSolutionItem");
          break;
        case VsProjectTypes.VsProjectItemKindPhysicalFolder:
        case VsProjectTypes.VsProjectItemKindSolutionFolder:
        case VsProjectTypes.UnloadedProjectTypeGuid:
          return; // not supported project kinds
        case VsProjectTypes.VsProjectKindMisc:
          isMiscFiles = true;
          break;
      }
      if (Constants.SolutionFolderGuid == new Guid(project.Kind))
        return; // not supported project kind

      var projectPath = project.FullName;
      var projectId = new ProjectId(_stringManager.GetId(projectPath));

      if (!isMiscFiles)
        Debug.Assert(!string.IsNullOrEmpty(projectPath));

      var isDelayLoading = _backgroundLoading != SolutionLoadingSate.AsynchronousLoading && !isMiscFiles;

      _projects.Add(project);

      foreach (var server in _servers)
        server.ProjectStartLoading(projectId, projectPath);

      Log.Message($"tr: AfterOpenProject(IsAdded='{e.IsAdded}', FullName='{projectPath}' id={projectId} Name={project.Name} State={_backgroundLoading})");

      foreach (var server in _servers)
        server.AddedMscorlibReference(projectId);

      if (!isDelayLoading)
      {
        foreach (var server in _servers)
          server.ProjectLoaded(GetProjectId(project));
      }
      else if (_backgroundLoading == SolutionLoadingSate.Loaded) // reloading or adding new project
      {
        try
        {
          ScanReferences(project);
        }
        catch (Exception ex)
        {
          Log.Exception(ex);
        }

        try
        {
          ScanFiles(project);
        }
        catch (Exception ex)
        {
          Log.Exception(ex);
        }

        foreach (var server in _servers)
          server.ProjectLoaded(GetProjectId(project));
      }
    }

    void SolutionEvents_OnBeforeCloseProject(object sender, CloseProjectEventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var hierarchy = e.Hierarchy;

      var project = hierarchy.GetProp<Project>(VSConstants.VSITEMID_ROOT, __VSHPROPID.VSHPROPID_ExtObject);
      var path    = project.FullName;
      var id      = new ProjectId(_stringManager.GetId(path));
      Log.Message($"tr: BeforeCloseProject(IsRemoved='{e.IsRemoved}', FullName='{project.FullName}' id={id})");

      if (_listenersMap.ContainsKey(hierarchy))
      {
        var listener = _listenersMap[hierarchy];
        listener.StopListening();
        listener.Dispose();
        _listenersMap.Remove(hierarchy);
      }


      if (project == null)
        return;

      _projects.Remove(project);
      _referenceMap.Remove(id);


      foreach (var server in _servers)
        server.BeforeCloseProject(id);
    }

    private void AddFile(ProjectItem projectItem, string path)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var ext         = Path.GetExtension(path);
      var id          = new FileId(_stringManager.GetId(path));
      var project     = projectItem.ContainingProject;
      var projectPath = project.FullName;
      var projectId   = new ProjectId(_stringManager.GetId(projectPath));

      Log.Message($"tr: AddFile(Name={projectItem.Name}, Id={id}, FileName='{path}', Project={project.Name}, ProjectId={projectId})");

      foreach (var server in _servers)
        if (server.IsSupportedExtension(ext))
          server.FileAdded(projectId, path, id, new FileVersion(), null);

      return;
    }

    void AfterOpeningChildren(object sender, HierarchyEventArgs e)
    {
      Log.Message($"tr: AfterOpeningChildren(Hierarchy='{e.Hierarchy}')");
    }

    void SolutionEvents_OnAfterMergeSolution(object sender, EventArgs e)
    {
      Log.Message($"tr: AfterMergeSolution()");
    }

    void AfterLoadProjectBatch(object sender, LoadProjectBatchEventArgs e)
    {
      Log.Message($"tr: AfterLoadProjectBatch(IsBackgroundIdleBatch='{e.IsBackgroundIdleBatch}')");
    }

    void AfterLoadProject(object sender, LoadProjectEventArgs e)
    {
      Log.Message($"tr: AfterLoadProject(RealHierarchy='{e.RealHierarchy}', StubHierarchy='{e.StubHierarchy}')");
    }

    void AfterClosingChildren(object sender, HierarchyEventArgs e)
    {
      Log.Message($"tr: AfterClosingChildren(Hierarchy='{e.Hierarchy}')");
    }

    void AfterCloseSolution(object sender, EventArgs e)
    {
      _projects.Clear();
      _listenersMap.Clear();
      _referenceMap.Clear();
      Debug.Assert(_currentSolutionId != InvalidSolutionId);
      _backgroundLoading = SolutionLoadingSate.NotLoaded;
      var path = _stringManager.GetPath(_currentSolutionId);
      Log.Message($"tr: AfterCloseSolution(Id={_currentSolutionId} Path='{path}')");
      _currentSolutionId = InvalidSolutionId;
    }

    void AfterChangeProjectParent(object sender, HierarchyEventArgs e)
    {
      Log.Message($"tr: AfterChangeProjectParent(Hierarchy='{e.Hierarchy}')");
    }

    void AfterAsynchOpenProject(object sender, OpenProjectEventArgs e)
    {
      Log.Message($"tr: AfterChangeProjectParent(Hierarchy='{e.Hierarchy}', IsAdded='{e.IsAdded}' _currentSolutionId={_currentSolutionId})");
    }

    void DocumentWindowOnScreenChanged(object sender, DocumentWindowOnScreenChangedEventArgs e)
    {
      var fullPath    = e.Info.FullPath;
      var ext         = Path.GetExtension(fullPath);
      var id          = new FileId(_stringManager.GetId(fullPath));
      var windowFrame = e.Info.WindowFrame;
      var vsTextView  = VsShellUtilities.GetTextView(windowFrame);
      var wpfTextView = vsTextView.ToIWpfTextView();
      if (wpfTextView == null)
        return;
      var dispatcher = wpfTextView.VisualElement.Dispatcher;
      var hierarchy = windowFrame.GetHierarchyFromVsWindowFrame();

      if (e.OnScreen)
      {
        Log.Message("OnScreen '" + fullPath + "'");
        foreach (var server in _servers)
          if (server.IsSupportedExtension(ext))
            server.ViewActivated(wpfTextView, id, hierarchy, fullPath);
      }
      else
      {
        Log.Message("OffScreen '" + fullPath + "'");
        foreach (var server in _servers)
          if (server.IsSupportedExtension(ext))
            server.ViewDeactivated(wpfTextView, id);
      }
    }

    void DocumentWindowDestroy(object sender, DocumentWindowEventArgs e)
    {
      var windowFrame = e.Info.WindowFrame;
      var vsTextView = VsShellUtilities.GetTextView(windowFrame);
      var wpfTextView = vsTextView.ToIWpfTextView();
      if (wpfTextView == null)
        return;
      foreach (var server in _servers)
        server.DocumentWindowDestroy(wpfTextView);
    }

    void SubscibeToSolutionEvents()
    {
      SolutionEvents.OnAfterAsynchOpenProject               += AfterAsynchOpenProject;
      SolutionEvents.OnAfterBackgroundSolutionLoadComplete  += AfterBackgroundSolutionLoadComplete;
      SolutionEvents.OnAfterChangeProjectParent             += AfterChangeProjectParent;
      SolutionEvents.OnAfterCloseSolution                   += AfterCloseSolution;
      SolutionEvents.OnAfterClosingChildren                 += AfterClosingChildren;
      SolutionEvents.OnAfterLoadProject                     += AfterLoadProject;
      SolutionEvents.OnAfterLoadProjectBatch                += AfterLoadProjectBatch;
      SolutionEvents.OnAfterMergeSolution                   += SolutionEvents_OnAfterMergeSolution;
      SolutionEvents.OnAfterOpeningChildren                 += AfterOpeningChildren;
      SolutionEvents.OnAfterOpenProject                     += AfterOpenProject;
      SolutionEvents.OnAfterOpenSolution                    += AfterOpenSolution;
      SolutionEvents.OnAfterRenameProject                   += AfterRenameProject;
      SolutionEvents.OnBeforeBackgroundSolutionLoadBegins   += BeforeBackgroundSolutionLoadBegins;
      SolutionEvents.OnBeforeCloseProject                   += SolutionEvents_OnBeforeCloseProject;
      SolutionEvents.OnBeforeCloseSolution                  += BeforeCloseSolution;
      SolutionEvents.OnBeforeClosingChildren                += BeforeClosingChildren;
      SolutionEvents.OnBeforeLoadProjectBatch               += BeforeLoadProjectBatch;
      SolutionEvents.OnBeforeOpeningChildren                += BeforeOpeningChildren;
      SolutionEvents.OnBeforeOpenProject                    += BeforeOpenProject;
      SolutionEvents.OnBeforeOpenSolution                   += BeforeOpenSolution;
      SolutionEvents.OnBeforeUnloadProject                  += BeforeUnloadProject;
      SolutionEvents.OnQueryBackgroundLoadProjectBatch      += QueryBackgroundLoadProjectBatch;
      SolutionEvents.OnQueryChangeProjectParent             += QueryChangeProjectParent;
      SolutionEvents.OnQueryCloseProject                    += QueryCloseProject;
      SolutionEvents.OnQueryCloseSolution                   += SolutionEvents_OnQueryCloseSolution;
      SolutionEvents.OnQueryUnloadProject                   += QueryUnloadProject;

      _runningDocTableEventse.DocumentWindowOnScreenChanged += DocumentWindowOnScreenChanged;
      _runningDocTableEventse.DocumentWindowDestroy         += DocumentWindowDestroy;
    }

    void UnsubscibeToSolutionEvents()
    {
      SolutionEvents.OnAfterAsynchOpenProject               -= AfterAsynchOpenProject;
      SolutionEvents.OnAfterBackgroundSolutionLoadComplete  -= AfterBackgroundSolutionLoadComplete;
      SolutionEvents.OnAfterChangeProjectParent             -= AfterChangeProjectParent;
      SolutionEvents.OnAfterCloseSolution                   -= AfterCloseSolution;
      SolutionEvents.OnAfterClosingChildren                 -= AfterClosingChildren;
      SolutionEvents.OnAfterLoadProject                     -= AfterLoadProject;
      SolutionEvents.OnAfterLoadProjectBatch                -= AfterLoadProjectBatch;
      SolutionEvents.OnAfterMergeSolution                   -= SolutionEvents_OnAfterMergeSolution;
      SolutionEvents.OnAfterOpeningChildren                 -= AfterOpeningChildren;
      SolutionEvents.OnAfterOpenProject                     -= AfterOpenProject;
      SolutionEvents.OnAfterOpenSolution                    -= AfterOpenSolution;
      SolutionEvents.OnAfterRenameProject                   -= AfterRenameProject;
      SolutionEvents.OnBeforeBackgroundSolutionLoadBegins   -= BeforeBackgroundSolutionLoadBegins;
      SolutionEvents.OnBeforeCloseProject                   -= SolutionEvents_OnBeforeCloseProject;
      SolutionEvents.OnBeforeCloseSolution                  -= BeforeCloseSolution;
      SolutionEvents.OnBeforeClosingChildren                -= BeforeClosingChildren;
      SolutionEvents.OnBeforeLoadProjectBatch               -= BeforeLoadProjectBatch;
      SolutionEvents.OnBeforeOpeningChildren                -= BeforeOpeningChildren;
      SolutionEvents.OnBeforeOpenProject                    -= BeforeOpenProject;
      SolutionEvents.OnBeforeOpenSolution                   -= BeforeOpenSolution;
      SolutionEvents.OnBeforeUnloadProject                  -= BeforeUnloadProject;
      SolutionEvents.OnQueryBackgroundLoadProjectBatch      -= QueryBackgroundLoadProjectBatch;
      SolutionEvents.OnQueryChangeProjectParent             -= QueryChangeProjectParent;
      SolutionEvents.OnQueryCloseProject                    -= QueryCloseProject;
      SolutionEvents.OnQueryCloseSolution                   -= SolutionEvents_OnQueryCloseSolution;
      SolutionEvents.OnQueryUnloadProject                   -= QueryUnloadProject;

      _runningDocTableEventse.DocumentWindowOnScreenChanged -= DocumentWindowOnScreenChanged;
      _runningDocTableEventse.DocumentWindowDestroy         -= DocumentWindowDestroy;
    }
  }
}
