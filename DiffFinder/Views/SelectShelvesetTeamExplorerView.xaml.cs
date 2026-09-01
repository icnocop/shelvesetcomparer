// <copyright file="SelectShelvesetTeamExplorerView.xaml.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

using EnvDTE;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace DiffFinder
{
    /// <summary>
    /// Team Explorer view allow to select shelveset for comparison.
    /// </summary>
    public partial class SelectShelvesetTeamExplorerView
    {
        /// <summary>
        /// Set while the selection of the shelveset list is being restored, so that the selection changes
        /// this causes are not mistaken for the user picking shelvesets.
        /// </summary>
        private bool restoringSelection;

        /// <summary>
        /// Initializes a new instance of the SelectShelvesetTeamExplorerView class
        /// </summary>
        /// <param name="parentSection">Reference to the Team Explorer section where the view is initialized.</param>
        public SelectShelvesetTeamExplorerView(SelectShelvesetSection parentSection)
        {
            this.InitializeComponent();
            this.ParentSection = parentSection;
            this.DataContext = this;

            // the shelvesets remember whether they were selected, so whenever the list is replaced - which
            // includes the section restoring the list it was left with - the selection is put back.
            // The generator raises this after the new items are in place, and unlike a
            // DependencyPropertyDescriptor it does not root the list in a static table.
            this.ListShelvesets.ItemContainerGenerator.ItemsChanged += this.OnShelvesetsChanged;
        }

        /// <summary>
        /// Gets the Team Explorer section in to which the view is created.
        /// </summary>
        public SelectShelvesetSection ParentSection
        {
            get;
            private set;
        }

        /// <summary>
        /// Event Handler for Selection change in the Shelvesets list
        /// </summary>
        /// <param name="sender">The ListShelvesets ListView control</param>
        /// <param name="e">The event arguments</param>
        public void ListShelvesetsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListShelvesets.SelectedItems.Count > 2)
            {
                this.ListShelvesets.SelectedItems.RemoveAt(0);
            }

            if (this.restoringSelection || e == null)
            {
                return;
            }

            // record the selection on the shelvesets themselves, so that it survives the section being
            // recreated when navigating away and back
            foreach (var item in e.RemovedItems.OfType<ShelvesetViewModel>())
            {
                item.IsSelected = false;
            }

            foreach (var item in e.AddedItems.OfType<ShelvesetViewModel>())
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// Event handler for the shelveset list being replaced, which happens both when the shelvesets are
        /// listed afresh and when the section restores the list it was left with. Reselects the shelvesets
        /// that were selected. Driving the selection from the items rather than binding ListViewItem.IsSelected
        /// keeps it working for rows the list has virtualized away, which have no container to bind to.
        /// </summary>
        /// <param name="sender">The item container generator of the shelveset list</param>
        /// <param name="e">The event arguments</param>
        private void OnShelvesetsChanged(object sender, ItemsChangedEventArgs e)
        {
            if (this.restoringSelection)
            {
                return;
            }

            // the items source is bound asynchronously, so let the list settle before selecting into it
            this.Dispatcher.BeginInvoke(new Action(this.RestoreSelection), DispatcherPriority.Background);
        }

        /// <summary>
        /// Selects the shelvesets in the list that are marked as selected.
        /// </summary>
        private void RestoreSelection()
        {
            try
            {
                this.restoringSelection = true;

                var selected = this.ListShelvesets.Items
                    .OfType<ShelvesetViewModel>()
                    .Where(shelveset => shelveset.IsSelected)
                    .Take(2)
                    .ToList();

                if (selected.Count == 0)
                {
                    return;
                }

                this.ListShelvesets.SelectedItems.Clear();
                foreach (var shelveset in selected)
                {
                    this.ListShelvesets.SelectedItems.Add(shelveset);
                }
            }
            catch (Exception)
            {
                this.ShowFailed();
            }
            finally
            {
                this.restoringSelection = false;
            }
        }

        /// <summary>
        /// Event Handler for selecting a user in one of the shelveset owner drop down lists.
        /// Shared by both drop down lists.
        /// </summary>
        /// <param name="sender">The shelveset user combo box</param>
        /// <param name="e">The event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Eventhandler + Exceptions handled")]
        private async void ShelvesetUserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // while the list of users is being refreshed the selection changes because the list is
                // being replaced, not because the user picked somebody
                if (this.ParentSection.IsRefreshingUsers)
                {
                    return;
                }

                this.ClearError();
                await this.ParentSection.RefreshAsync();
            }
            catch (Exception)
            {
                this.ShowFailed();
            }
        }

        /// <summary>
        /// Event Handler for the refresh users button.
        /// </summary>
        /// <param name="sender">The refresh users button</param>
        /// <param name="e">Event parameters</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Eventhandler + Exceptions handled")]
        private async void RefreshUsersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearError();
                await this.ParentSection.RefreshUsersAsync();
            }
            catch (Exception)
            {
                this.ShowFailed();
            }
        }

        private void ShowFailed([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            ShowError($"Failed to {caller}");
        }

        /// <summary>
        /// Displays the error panel
        /// </summary>
        /// <param name="error">The error text</param>
        private void ShowError(string error)
        {
            this.ErrorText.Text = error;
            this.ErrorPanel.Visibility = System.Windows.Visibility.Visible;
            ShelvesetComparer.OutputPaneWriteLine(ParentSection.ServiceProvider, error);
        }

        /// <summary>
        /// Clears the error panel
        /// </summary>
        private void ClearError()
        {
            this.ErrorText.Text = string.Empty;
            this.ErrorPanel.Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// Event Handler for the list button.
        /// </summary>
        /// <param name="sender">The list button</param>
        /// <param name="e">Event parameters</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Eventhandler + Exceptions handled")]
        private async void ListButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearError();
                await this.ParentSection.RefreshAsync();
            }
            catch (Exception)
            {
                this.ShowFailed();
            }
        }

        /// <summary>
        /// Event Handler for the compare button.
        /// </summary>
        /// <param name="sender">The compare button</param>
        /// <param name="e">Event parameters</param>
        private void CompareButtons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearError();
                if (this.ListShelvesets.SelectedItems != null && this.ListShelvesets.SelectedItems.Count != 2)
                {
                    this.ShowError(DiffFinder.Resources.ShelvesetsNotSelectedErrorMessage);
                    return;
                }
                
                var firstSheleveset = this.ListShelvesets.SelectedItems[0] as ShelvesetViewModel;
                var secondSheleveset = this.ListShelvesets.SelectedItems[1] as ShelvesetViewModel;
                ShelvesetComparerViewModel.Instance.Initialize(firstSheleveset, secondSheleveset);

                if (ShelvesetComparer.Instance != null)
                {
                    ShelvesetComparer.Instance.ShowComparisonToolWindow();
                }
                else
                {
                    // if the package has not yet been initialized, then we need to call it via DTE
                    var dte2 = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
                    dte2?.ExecuteCommand(ShelvesetComparer.ShelvesetComparerResuldIdDteCommandName);
                }
            }
            catch (Exception ex)
            {
                // write full exception to output
                ShelvesetComparer.Instance?.OutputPaneWriteLine(ex.ToString());

                this.ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Event Handler for the compare button.
        /// </summary>
        /// <param name="sender">The compare button</param>
        /// <param name="e">Event parameters</param>
        private void CompareWithPendChangeButtons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearError();
                if (this.ListShelvesets.SelectedItems != null && this.ListShelvesets.SelectedItems.Count != 1)
                {
                    this.ShowError(DiffFinder.Resources.ShelvesetNotSelectedErrorMessage);
                    return;
                }

                // get workspace from page
                var parent = this.ParentSection;
                var page = parent.TeamExplorer.GetCurrentPageAsShelvesetComparerPage();
                var pendChangeShelveset = parent.FetchPendingChangeShelveset(this.ParentSection.Context, page?.CurrentWorkspace);
                
                var firstSheleveset = this.ListShelvesets.SelectedItems[0] as ShelvesetViewModel;
                var secondSheleveset = pendChangeShelveset;
                ShelvesetComparerViewModel.Instance.Initialize(firstSheleveset, secondSheleveset);

                if (ShelvesetComparer.Instance != null)
                {
                    ShelvesetComparer.Instance.ShowComparisonToolWindow();
                }
                else
                {
                    // if the package has not yet been initialized, then we need to call it via DTE
                    var dte2 = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
                    dte2?.ExecuteCommand(ShelvesetComparer.ShelvesetComparerResuldIdDteCommandName);
                }
            }
            catch (Exception ex)
            {
                // write full exception to output
                ShelvesetComparer.Instance?.OutputPaneWriteLine(ex.ToString());

                this.ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Event Handler for mouse double click on the shelvesets list.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void ListShelvesets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            // the list selects with Multiple mode, where a click toggles, so the selection after a double
            // click depends on what was selected before it. Use the row that was actually clicked instead.
            var container = ItemsControl.ContainerFromElement(this.ListShelvesets, e.OriginalSource as DependencyObject) as ListViewItem;
            this.ViewShelvesetDetails(CastAsShelveset(container?.DataContext));
        }
       
        /// <summary>
        /// Event Handler for key up event on the shelvesets list.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void ListShelvesets_KeyUp(object sender, KeyEventArgs e)
        {
            if (e != null && e.Key == Key.Enter && this.ListShelvesets.SelectedItems.Count == 1)
            {
                this.ViewShelvesetDetails(CastAsShelveset(this.ListShelvesets.SelectedItems[0]));
            }
        }

        /// <summary>
        /// Opens up the shelveset details team explorer page for given shelveset
        /// </summary>
        /// <param name="shelveset">The shelveset to show the details of. Ignored when null.</param>
        private void ViewShelvesetDetails(Shelveset shelveset)
        {
            if (shelveset == null)
            {
                return;
            }

            try
            {
                this.ClearError();
                this.ParentSection.ViewShelvesetDetails(shelveset);
            }
            catch (Exception ex)
            {
                // the navigation used to fail silently, which gave no clue why a shelveset would not open
                ShelvesetComparer.Instance?.OutputPaneWriteLine(ex.ToString());
                this.ShowError($"Failed to open the details of shelveset '{shelveset.Name}' owned by '{shelveset.OwnerName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Cast given item to <see cref="ShelvesetViewModel"/> and return wrapped MS <see cref="Shelveset"/>.
        /// </summary>
        /// <param name="listViewSelectedItem">Object to cast and get Shelveset from</param>
        /// <returns>Wraped <see cref="Shelveset"/> or null</returns>
        private static Shelveset CastAsShelveset(object listViewSelectedItem)
        {
            return (listViewSelectedItem as ShelvesetViewModel)?.Shelveset;
        }
    }
}
