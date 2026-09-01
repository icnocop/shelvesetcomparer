// <copyright file="MainView.xaml.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>


using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiffFinder
{
    /// <summary>
    /// The Main View of the shelveset comparison window.
    /// </summary>
    public partial class MainView : UserControl
    {
        /// <summary>
        /// The dependency property containing for the Shelveset Comparison View Model
        /// </summary>
        private static readonly DependencyProperty ComparisonModelProperty = DependencyProperty.Register("ComparisonModel", typeof(ShelvesetComparerViewModel), typeof(MainView));

        /// <summary>
        /// Keeps the visual studio version
        /// </summary>
        private static string visualStudioVersion = string.Empty;

        /// <summary>
        /// Initializes a new instance of the MainView class.
        /// </summary>
        public MainView()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.ComparisonModel = ShelvesetComparerViewModel.Instance;
        }

        /// <summary>
        /// Gets the Visual Studio Version the extension is currently running in
        /// </summary>
        public static string VisualStudioVersion
        {
            get
            {
                if (string.IsNullOrWhiteSpace(visualStudioVersion))
                {
                    visualStudioVersion = GetVisualStudioVersionAsync().GetResultNoContext();
                }

                return visualStudioVersion;
            }
        }

        /// <summary>
        /// Gets or sets the ComparisonModel
        /// </summary>
        public ShelvesetComparerViewModel ComparisonModel
        {
            get
            {
                return this.GetValue(ComparisonModelProperty) as ShelvesetComparerViewModel;
            }

            set
            {
                this.SetValue(ComparisonModelProperty, value);
            }
        }

        /// <summary>
        /// Get Visual Studio version (enforcing Main UI Thread if required)
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GetVisualStudioVersionAsync()
        {
            if (! ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            return dte.SourceControl.Parent.Version;
        }

        /// <summary>
        /// The method opens up a window comparing two files
        /// </summary>
        /// <param name="compareFiles">The compare files view model</param>
        private static void CompareFiles(FileComparisonViewModel compareFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetFileToCompare(compareFiles.FirstFileDisplayName, compareFiles.FirstFile, out var firstFileName, out var extension, out var firstDisplayName, out var firstIsTemporary);
            GetFileToCompare(compareFiles.SecondFileDisplayName, compareFiles.SecondFile, out var secondFileName, out extension, out var secondDisplayName, out var secondIsTemporary);

            GetExternalTool(extension, out var diffToolCommand, out var diffToolCommandArguments);

            if (string.IsNullOrWhiteSpace(diffToolCommand))
            {
                OpenVisualStudioDiff(firstFileName, secondFileName, firstDisplayName, secondDisplayName, firstIsTemporary, secondIsTemporary);
            }
            else
            {
                // So there is a tool configured. Let's use it
                diffToolCommandArguments = DiffToolArgumentBuilder.Build(
                    diffToolCommandArguments,
                    firstFileName,
                    secondFileName,
                    firstDisplayName,
                    secondDisplayName);
                var startInfo = new ProcessStartInfo()
                {
                    Arguments = diffToolCommandArguments,
                    FileName = diffToolCommand
                };

                Process.Start(startInfo);
            }
        }

        private static void GetFileToCompare(string localFilePath, IPendingChange pendingChange, out string fileToDiff, out string extension, out string displayName, out bool isTemporary)
        {
            fileToDiff = localFilePath;
            displayName = fileToDiff;
            extension = null;
            isTemporary = false;
            if (! File.Exists(fileToDiff))
            {
                // if not existing locally, then use temp file for comparison and download server item
                fileToDiff = Path.GetTempFileName();
                isTemporary = true;
                if (pendingChange != null)
                {
                    pendingChange.DownloadShelvedFile(fileToDiff);
                    extension = Path.GetExtension(pendingChange.FileName);
                    displayName = $"{pendingChange.ServerItem};{pendingChange.Version}";
                }
            }
            else
            {
                extension = Path.GetExtension(fileToDiff);
            }
        }

        /// <summary>
        /// Opens the two files in the difference window of the Visual Studio instance the extension is
        /// running in. Launching devenv.exe /diff instead would hand the comparison to whichever instance
        /// happens to be registered, which is not necessarily this one.
        /// </summary>
        /// <param name="firstFileName">The path of the first file</param>
        /// <param name="secondFileName">The path of the second file</param>
        /// <param name="firstDisplayName">The label of the first file</param>
        /// <param name="secondDisplayName">The label of the second file</param>
        /// <param name="firstIsTemporary">Whether the first file is a temporary file Visual Studio may delete</param>
        /// <param name="secondIsTemporary">Whether the second file is a temporary file Visual Studio may delete</param>
        private static void OpenVisualStudioDiff(string firstFileName, string secondFileName, string firstDisplayName, string secondDisplayName, bool firstIsTemporary, bool secondIsTemporary)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var differenceService = Package.GetGlobalService(typeof(SVsDifferenceService)) as IVsDifferenceService;
            if (differenceService == null)
            {
                throw new InvalidOperationException("The Visual Studio difference service is not available.");
            }

            var caption = string.Format(CultureInfo.CurrentCulture, "{0} vs. {1}", Path.GetFileName(firstDisplayName), Path.GetFileName(secondDisplayName));

            uint options = 0;
            if (firstIsTemporary)
            {
                options |= (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary;
            }

            if (secondIsTemporary)
            {
                options |= (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_RightFileIsTemporary;
            }

            differenceService.OpenComparisonWindow2(
                firstFileName,
                secondFileName,
                caption,
                $"{firstDisplayName}\r\n{secondDisplayName}",
                firstDisplayName,
                secondDisplayName,
                null,
                null,
                options);
        }

        /// <summary>
        /// Returns the file path of the external tool configured for comparison for the file with given extension.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <param name="diffToolCommand">If a comparison tool is found this will contain the path of the tool</param>
        /// <param name="diffToolCommandArguments">If a comparison tool is found this will contain command line arguments for the tool</param>
        private static void GetExternalTool(string extension, out string diffToolCommand, out string diffToolCommandArguments)
        {
            diffToolCommand = string.Empty;
            diffToolCommandArguments = string.Empty;

            // read registry key for the extension
            diffToolCommand = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\" + VisualStudioVersion + @"\TeamFoundation\SourceControl\DiffTools\" + extension + @"\Compare", "Command", null);
            diffToolCommandArguments = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\" + VisualStudioVersion + @"\TeamFoundation\SourceControl\DiffTools\" + extension + @"\Compare", "Arguments", null);
            if (diffToolCommand != null && diffToolCommandArguments != null)
            {
                return;
            }

            // read registry key for the wildcard
            diffToolCommand = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\" + VisualStudioVersion + @"\TeamFoundation\SourceControl\DiffTools\.*\Compare", "Command", null);
            diffToolCommandArguments = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\" + VisualStudioVersion + @"\TeamFoundation\SourceControl\DiffTools\.*\Compare", "Arguments", null);
        }

        /// <summary>
        /// Event Handler for Mouse Double click event 
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e != null && e.ChangedButton == MouseButton.Left)
            {
                if (this.ComparisonFiles.SelectedItem is FileComparisonViewModel compareFiles)
                {
                    CompareFiles(compareFiles);
                }
            }
        }

        /// <summary>
        /// Event Handler for Key up event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e != null && e.Key == Key.Enter)
            {
                if (this.ComparisonFiles.SelectedItem is FileComparisonViewModel compareFiles)
                {
                    CompareFiles(compareFiles);
                }
            }
        }

        /// <summary>
        /// Key up event for the search dialog
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void SearchFilesTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            ShelvesetComparerViewModel.Instance.Filter = this.SearchFilesTextBox.Text;
        }
    }
}
