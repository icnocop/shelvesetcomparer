// <copyright file="MainView.xaml.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>


using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        /// The row and side of the file picked with "Compare to...", waiting for the file to compare it
        /// with. Null when no file is waiting.
        /// </summary>
        private FileComparisonViewModel compareToSource;

        /// <summary>
        /// The side of <see cref="compareToSource"/>.
        /// </summary>
        private ComparisonSide compareToSourceSide;

        /// <summary>
        /// The row the context menu was opened on, null when it was opened outside of a row.
        /// </summary>
        private FileComparisonViewModel contextMenuRow;

        /// <summary>
        /// The side of the cell the context menu was opened on.
        /// </summary>
        private ComparisonSide contextMenuSide;

        /// <summary>
        /// Set when a "Compare to..." comparison was opened on the first click of what may become a double
        /// click, so that the double click does not also compare the files of the row clicked.
        /// </summary>
        private bool suppressDoubleClick;

        /// <summary>
        /// Initializes a new instance of the MainView class.
        /// </summary>
        public MainView()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.ComparisonModel = ShelvesetComparerViewModel.Instance;

            // a file picked from the previous comparison is not one the user would compare to the files of
            // the next. The view model is a singleton that outlives the view, hence the weak subscription.
            PropertyChangedEventManager.AddHandler(this.ComparisonModel, this.OnShelvesetNameChanged, nameof(ShelvesetComparerViewModel.FirstShelvesetName));
            PropertyChangedEventManager.AddHandler(this.ComparisonModel, this.OnShelvesetNameChanged, nameof(ShelvesetComparerViewModel.SecondShelvesetName));
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
        /// The method opens up a window comparing two files
        /// </summary>
        /// <param name="compareFiles">The compare files view model</param>
        private static void CompareFiles(FileComparisonViewModel compareFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetFileToCompare(compareFiles.FirstFileDisplayName, compareFiles.FirstFile, out var firstFileName, out _, out var firstDisplayName, out var firstIsTemporary);
            GetFileToCompare(compareFiles.SecondFileDisplayName, compareFiles.SecondFile, out var secondFileName, out _, out var secondDisplayName, out var secondIsTemporary);

            if (ThirdPartyRunner.IsCompareToolConfigured(secondFileName))
            {
                // Team Foundation owns the difference tools the user configured under Options, Source
                // Control, Visual Studio Team Foundation Server, Configure User Tools: it resolves the one
                // for the extension per user and per machine, handles tools implemented as an assembly
                // rather than an executable, substitutes and quotes the arguments, and deletes the
                // temporary files once the tool exits.
                Difference.VisualDiffFiles(
                    firstFileName,
                    secondFileName,
                    firstDisplayName,
                    secondDisplayName,
                    compareFiles.FirstShelveName ?? string.Empty,
                    compareFiles.SecondShelveName ?? string.Empty,
                    firstIsTemporary,
                    secondIsTemporary,
                    firstIsTemporary,
                    secondIsTemporary);
                return;
            }

            // no tool is configured, which Difference reports by throwing rather than by falling back, so
            // the built in difference window is ours to open
            OpenVisualStudioDiff(
                firstFileName,
                secondFileName,
                AppendShelvesetName(firstDisplayName, compareFiles.FirstShelveName),
                AppendShelvesetName(secondDisplayName, compareFiles.SecondShelveName),
                firstIsTemporary,
                secondIsTemporary);
        }

        /// <summary>
        /// Qualifies the label of a file with the shelveset it came from, so that the two sides of the
        /// comparison can be told apart when the same file is shelved in both.
        /// </summary>
        /// <param name="displayName">The label of the file</param>
        /// <param name="shelvesetName">The name of the shelveset the file was shelved in</param>
        /// <returns>The qualified label</returns>
        private static string AppendShelvesetName(string displayName, string shelvesetName)
        {
            return string.IsNullOrWhiteSpace(shelvesetName) ? displayName : $"{displayName};{shelvesetName}";
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
                isTemporary = true;
                if (pendingChange != null)
                {
                    // keep the extension of the shelved file: the difference window picks the language
                    // service from it, so a .tmp would lose the syntax highlighting
                    extension = Path.GetExtension(pendingChange.FileName);
                    fileToDiff = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
                    pendingChange.DownloadShelvedFile(fileToDiff);
                    displayName = $"{pendingChange.ServerItem};{pendingChange.Version}";
                }
                else
                {
                    fileToDiff = Path.GetTempFileName();
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

            var frame = differenceService.OpenComparisonWindow2(
                firstFileName,
                secondFileName,
                caption,
                $"{firstDisplayName}\r\n{secondDisplayName}",
                firstDisplayName,
                secondDisplayName,
                null,
                null,
                options);

            // the window is shown unless VSDIFFOPT_DoNotShow is passed, so this only brings it forward
            frame?.Show();
        }


        /// <summary>
        /// Opens the comparison, reporting a failure rather than letting it escape into the event handler.
        /// Difference reports a missing file or a comparison tool that cannot be run by throwing, so the
        /// message it carries is the only clue the user gets.
        /// </summary>
        /// <param name="compareFiles">The compare files view model</param>
        private static void CompareFilesReportingFailure(FileComparisonViewModel compareFiles)
        {
            try
            {
                CompareFiles(compareFiles);
            }
            catch (Exception ex)
            {
                ShelvesetComparer.Instance?.OutputPaneWriteLine(ex.ToString());
                MessageBox.Show(ex.Message, DiffFinder.Resources.ToolWindowTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event Handler for Mouse Double click event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.suppressDoubleClick)
            {
                this.suppressDoubleClick = false;
                return;
            }

            if (e != null && e.ChangedButton == MouseButton.Left)
            {
                if (this.ComparisonFiles.SelectedItem is FileComparisonViewModel compareFiles)
                {
                    CompareFilesReportingFailure(compareFiles);
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
                    CompareFilesReportingFailure(compareFiles);
                }
            }
            else if (e != null && e.Key == Key.Escape)
            {
                this.CancelCompareTo();
            }
        }

        /// <summary>
        /// Finds the cell of the comparison grid the given element is part of.
        /// </summary>
        /// <param name="element">The element, typically the source of a mouse event</param>
        /// <param name="row">The row of the cell</param>
        /// <param name="side">The side of the cell</param>
        /// <returns>True when the element is part of a cell, false otherwise</returns>
        private static bool TryGetCell(DependencyObject element, out FileComparisonViewModel row, out ComparisonSide side)
        {
            for (var current = element; current != null; current = GetParent(current))
            {
                if (current is FrameworkElement cell && cell.Tag is ComparisonSide cellSide && cell.DataContext is FileComparisonViewModel cellRow)
                {
                    row = cellRow;
                    side = cellSide;
                    return true;
                }

                if (current is ListViewItem)
                {
                    break;
                }
            }

            row = null;
            side = ComparisonSide.First;
            return false;
        }

        /// <summary>
        /// Returns the parent of the element, walking up through text elements such as a Run, which are not
        /// part of the visual tree.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The parent, or null at the root</returns>
        private static DependencyObject GetParent(DependencyObject element)
        {
            return element is Visual || element is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        /// <summary>
        /// Enables the context menu items that apply to the cell the menu is opened on.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!TryGetCell(e?.OriginalSource as DependencyObject, out this.contextMenuRow, out this.contextMenuSide))
            {
                this.contextMenuRow = null;
            }

            var file = this.contextMenuRow?.GetFile(this.contextMenuSide);
            this.CompareMenuItem.IsEnabled = this.contextMenuRow != null;
            this.CompareToMenuItem.IsEnabled = file != null;
            // an underscore in the file name would be taken for an access key unless doubled
            this.CompareToMenuItem.Header = file == null ? "Compare _to..." : $"Compare '{file.FileName.Replace("_", "__")}' _to...";
            this.CancelCompareToMenuItem.Visibility = this.compareToSource == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Event Handler for the compare context menu item, comparing the files of the row like a double click.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void CompareMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.contextMenuRow != null)
            {
                CompareFilesReportingFailure(this.contextMenuRow);
            }
        }

        /// <summary>
        /// Event Handler for the compare to context menu item. Remembers the file the menu was opened on, so
        /// that the next file clicked is compared to it.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void CompareToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var file = this.contextMenuRow?.GetFile(this.contextMenuSide);
            if (file == null)
            {
                return;
            }

            this.compareToSource = this.contextMenuRow;
            this.compareToSourceSide = this.contextMenuSide;

            var shelveName = this.compareToSource.GetShelveName(this.compareToSourceSide);
            this.CompareToHintText.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Click the file to compare '{0}'{1} to, or press Esc to cancel.",
                file.FileName,
                string.IsNullOrWhiteSpace(shelveName) ? string.Empty : $" of shelveset '{shelveName}'");
            this.CompareToHint.Visibility = Visibility.Visible;
            this.ComparisonFiles.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Event Handler for the cancel compare to context menu item.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void CancelCompareToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.CancelCompareTo();
        }

        /// <summary>
        /// Event Handler for a click on the comparison grid. While a file picked with "Compare to..." is
        /// waiting, compares it to the file clicked.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.compareToSource == null || e == null)
            {
                return;
            }

            // a click outside of a file, such as on a column header or an empty cell, keeps waiting
            if (!TryGetCell(e.OriginalSource as DependencyObject, out var row, out var side))
            {
                return;
            }

            var file = row.GetFile(side);
            if (file == null || ReferenceEquals(file, this.compareToSource.GetFile(this.compareToSourceSide)))
            {
                return;
            }

            var compareFiles = FileComparisonViewModel.CreateUnaligned(this.compareToSource, this.compareToSourceSide, row, side);
            this.CancelCompareTo();
            this.suppressDoubleClick = true;
            e.Handled = true;
            CompareFilesReportingFailure(compareFiles);
        }

        /// <summary>
        /// Event Handler for a mouse press on the comparison grid. A press that starts a new click, rather
        /// than completing a double click, means no double click follows the "Compare to..." comparison.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event Argument</param>
        private void ComparisonFiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e != null && e.ClickCount == 1)
            {
                this.suppressDoubleClick = false;
            }
        }

        /// <summary>
        /// Stops waiting for the file to compare the file picked with "Compare to..." with.
        /// </summary>
        private void CancelCompareTo()
        {
            this.compareToSource = null;
            this.CompareToHint.Visibility = Visibility.Collapsed;
            this.CompareToHintText.Text = string.Empty;
            this.ComparisonFiles.ClearValue(CursorProperty);
        }

        /// <summary>
        /// Cancels "Compare to..." when another pair of shelvesets is compared.
        /// </summary>
        /// <param name="sender">The comparison view model</param>
        /// <param name="e">Event Argument</param>
        private void OnShelvesetNameChanged(object sender, PropertyChangedEventArgs e)
        {
            this.CancelCompareTo();
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
