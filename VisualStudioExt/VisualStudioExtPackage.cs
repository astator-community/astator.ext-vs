using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

namespace VisualStudioExt
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VisualStudioExtPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(Pages.DebugWindow), Style = VsDockStyle.Tabbed, Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}")]
    public sealed class VisualStudioExtPackage : AsyncPackage
    {
        public static DTE2 DTE2 { get; private set; }

        /// <summary>
        /// VisualStudioExtPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "dc97d3d6-d2ae-462c-bd89-a0063fdc7ed9";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await Pages.DebugWindowCommand.InitializeAsync(this);

            DTE2 = await this.GetServiceAsync(typeof(DTE)) as DTE2;
        }

        #endregion

        public static string GetActiveDocumentName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (DTE2.ActiveDocument != null)
            {
                var document = DTE2.ActiveDocument;
                return document.ActiveWindow.Document.Name;
            }
            return null;
        }

        public static string GetProjectDir()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = DTE2.ActiveSolutionProjects as Array;

            if (projects.Length > 0)
            {
                var project = projects.GetValue(0) as Project;
                project.Save();
                var fullName = project?.FullName ?? string.Empty;
                var rootDir = Path.GetDirectoryName(fullName);
                return rootDir;
            }

            return string.Empty;
        }
    }
}
