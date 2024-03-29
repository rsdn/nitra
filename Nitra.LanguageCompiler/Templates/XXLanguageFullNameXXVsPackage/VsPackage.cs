﻿#pragma warning disable VSSDK002 // Visual Studio service should be used on main thread explicitly.

namespace XXNamespaceXX
{
    using Microsoft;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Events;
    using Microsoft.VisualStudio.Shell.Interop;

    using NitraCommonIde;

    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// This class implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// This package is required if you want to define adds custom commands (ctmenu)
    /// or localized resources for the strings that appear in the New Project and Open Project dialogs.
    /// Creating project extensions or project types does not actually require a VSPackage.
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Description("Nitra Package for XXLanguageFullNameXX language.")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(XXLanguageXXGuids.PackageGuid)]
    // This attribute is used to register the information needed to show this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideBindingPath(SubPath = "Languages")]
    public sealed class VsPackage : AsyncPackage
    {
        public static VsPackage Instance;

        static VsPackage()
        {
        }

        public VsPackage()
        {
            Instance = this;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var nitraInit = (INitraInit)await GetServiceAsync(typeof(INitraInit)).ConfigureAwait(false);
            Assumes.Present(nitraInit);

            var assembly = "XXProjectSupportAssemblyXX";

            if (string.IsNullOrEmpty(assembly))
                return;

            var assemblyFullPath = Path.Combine(VsUtils.GetPluginPath(), "Languages", assembly);
            var projectSupport   = new ProjectSupport("XXProjectSupportXX", "XXProjectSupportClassXX", assemblyFullPath);
            var extensions       = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase, XXFileExtensionsXX);
            var languages        = new[] { new LanguageInfo("XXLanguageFullNameXX", assemblyFullPath, extensions) };
            var config           = new Config(projectSupport, languages);

            await nitraInit.Init(cancellationToken, config).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}

#pragma warning restore VSSDK002 // Visual Studio service should be used on main thread explicitly.
