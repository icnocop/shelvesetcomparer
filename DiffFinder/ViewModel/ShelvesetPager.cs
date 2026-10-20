namespace DiffFinder
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Lists the shelvesets a page at a time, so that the details of a shelveset are only read once its page
    /// is shown. Sorting applies to all the shelvesets, not only to the page shown, and the selection is
    /// kept across the pages, so that two shelvesets on different pages can be compared.
    /// </summary>
    public class ShelvesetPager : INotifyPropertyChanged
    {
        /// <summary>
        /// The number of shelvesets a page lists unless told otherwise
        /// </summary>
        public const int DefaultPageSize = 25;

        /// <summary>
        /// The number of shelvesets that can be selected at once, which is what a comparison takes
        /// </summary>
        private const int MaximumSelection = 2;

        /// <summary>
        /// All the shelvesets, in the order they are listed in
        /// </summary>
        private List<ShelvesetViewModel> items = new List<ShelvesetViewModel>();

        /// <summary>
        /// The selected shelvesets, in the order they were selected in
        /// </summary>
        private readonly List<ShelvesetViewModel> selected = new List<ShelvesetViewModel>();

        /// <summary>
        /// The index of the page shown, starting at zero
        /// </summary>
        private int pageIndex;

        /// <summary>
        /// Initializes a new instance of the ShelvesetPager class.
        /// </summary>
        /// <param name="pageSize">The number of shelvesets a page lists</param>
        public ShelvesetPager(int pageSize = DefaultPageSize)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            this.PageSize = pageSize;
            this.CurrentPage = new ShelvesetViewModel[0];
            this.NextPageCommand = new DelegateCommand(_ => this.Next(), _ => this.CanGoNext);
            this.PreviousPageCommand = new DelegateCommand(_ => this.Previous(), _ => this.CanGoPrevious);
            this.SortCommand = new DelegateCommand(propertyName => this.Sort(propertyName as string), propertyName => propertyName is string);
        }

        /// <summary>
        /// Notification event used by the list to update itself when the page changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when a page is shown that lists shelvesets whose details are not loaded yet.
        /// </summary>
        public event EventHandler<PageShownEventArgs> PageShown;

        /// <summary>
        /// Gets the number of shelvesets a page lists.
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// Gets the command showing the next page.
        /// </summary>
        public DelegateCommand NextPageCommand { get; private set; }

        /// <summary>
        /// Gets the command showing the previous page.
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; private set; }

        /// <summary>
        /// Gets the command sorting all the shelvesets by the property named by the command parameter, which
        /// is what clicking a column header of the list does.
        /// </summary>
        public DelegateCommand SortCommand { get; private set; }

        /// <summary>
        /// Gets all the shelvesets, in the order they are listed in.
        /// </summary>
        public IReadOnlyList<ShelvesetViewModel> Items
        {
            get
            {
                return this.items.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the shelvesets of the page shown.
        /// </summary>
        public IReadOnlyList<ShelvesetViewModel> CurrentPage { get; private set; }

        /// <summary>
        /// Gets the index of the page shown, starting at zero.
        /// </summary>
        public int PageIndex
        {
            get
            {
                return this.pageIndex;
            }
        }

        /// <summary>
        /// Gets the number of pages, at least one even when there is no shelveset.
        /// </summary>
        public int PageCount
        {
            get
            {
                return Math.Max(1, (this.items.Count + this.PageSize - 1) / this.PageSize);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a page before the one shown.
        /// </summary>
        public bool CanGoPrevious
        {
            get
            {
                return this.pageIndex > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a page after the one shown.
        /// </summary>
        public bool CanGoNext
        {
            get
            {
                return this.pageIndex < this.PageCount - 1;
            }
        }

        /// <summary>
        /// Gets the text telling which page is shown, such as "Page 2 of 5 (112 shelvesets)".
        /// </summary>
        public string PageText
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, "Page {0} of {1} ({2} shelvesets)", this.pageIndex + 1, this.PageCount, this.items.Count);
            }
        }

        /// <summary>
        /// Gets the selected shelvesets, in the order they were selected in, whichever page they are on.
        /// </summary>
        public IReadOnlyList<ShelvesetViewModel> SelectedShelvesets
        {
            get
            {
                return this.selected.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the text naming the selected shelvesets, which tells about a selection on another page.
        /// Empty when nothing is selected.
        /// </summary>
        public string SelectionText
        {
            get
            {
                return this.selected.Count == 0 ? string.Empty : "Selected: " + string.Join(", ", this.selected.Select(s => s.Name));
            }
        }

        /// <summary>
        /// Gets the name of the property the shelvesets are sorted by, null when they are listed in the
        /// order they were given in.
        /// </summary>
        public string SortPropertyName { get; private set; }

        /// <summary>
        /// Gets the direction the shelvesets are sorted in.
        /// </summary>
        public ListSortDirection SortDirection { get; private set; }

        /// <summary>
        /// Replaces the shelvesets, keeping the sort order. The shelvesets marked as selected become the
        /// selection, which is how the selection is restored along with the list.
        /// </summary>
        /// <param name="shelvesets">The shelvesets</param>
        public void SetItems(IEnumerable<ShelvesetViewModel> shelvesets)
        {
            this.items = (shelvesets ?? Enumerable.Empty<ShelvesetViewModel>()).Where(s => s != null).ToList();
            this.ApplySort();

            this.selected.Clear();
            this.selected.AddRange(this.items.Where(s => s.IsSelected).Take(MaximumSelection));
            foreach (var extra in this.items.Where(s => s.IsSelected).Skip(MaximumSelection))
            {
                extra.IsSelected = false;
            }

            this.ShowPage(Math.Min(this.pageIndex, this.PageCount - 1));
            this.OnSelectionChanged();
        }

        /// <summary>
        /// Shows the next page, if there is one.
        /// </summary>
        public void Next()
        {
            if (this.CanGoNext)
            {
                this.ShowPage(this.pageIndex + 1);
            }
        }

        /// <summary>
        /// Shows the previous page, if there is one.
        /// </summary>
        public void Previous()
        {
            if (this.CanGoPrevious)
            {
                this.ShowPage(this.pageIndex - 1);
            }
        }

        /// <summary>
        /// Shows the page of the given index, or the nearest page there is.
        /// </summary>
        /// <param name="index">The index of the page, starting at zero</param>
        public void GoToPage(int index)
        {
            this.ShowPage(Math.Min(Math.Max(0, index), this.PageCount - 1));
        }

        /// <summary>
        /// Sorts all the shelvesets by the given property, ascending, or descending when they are sorted by
        /// that property ascending already, and shows the first page.
        /// </summary>
        /// <param name="propertyName">The name of the property of <see cref="ShelvesetViewModel"/></param>
        public void Sort(string propertyName)
        {
            this.SetSort(
                propertyName,
                this.SortPropertyName == propertyName && this.SortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending);
        }

        /// <summary>
        /// Sorts all the shelvesets by the given property in the given direction, and shows the first page.
        /// </summary>
        /// <param name="propertyName">The name of the property of <see cref="ShelvesetViewModel"/>, null to
        /// keep the order the shelvesets were given in</param>
        /// <param name="direction">The direction to sort in</param>
        public void SetSort(string propertyName, ListSortDirection direction)
        {
            this.SortDirection = direction;
            this.SortPropertyName = propertyName;
            this.ApplySort();

            this.OnPropertyChanged(nameof(this.SortPropertyName));
            this.OnPropertyChanged(nameof(this.SortDirection));
            this.ShowPage(0);
        }

        /// <summary>
        /// Selects the shelveset. Selecting more shelvesets than a comparison takes deselects the one
        /// selected first, whichever page it is on.
        /// </summary>
        /// <param name="shelveset">The shelveset</param>
        public void Select(ShelvesetViewModel shelveset)
        {
            if (shelveset == null || this.selected.Contains(shelveset))
            {
                return;
            }

            this.selected.Add(shelveset);
            shelveset.IsSelected = true;
            while (this.selected.Count > MaximumSelection)
            {
                this.selected[0].IsSelected = false;
                this.selected.RemoveAt(0);
            }

            this.OnSelectionChanged();
        }

        /// <summary>
        /// Deselects the shelveset.
        /// </summary>
        /// <param name="shelveset">The shelveset</param>
        public void Deselect(ShelvesetViewModel shelveset)
        {
            if (shelveset == null)
            {
                return;
            }

            shelveset.IsSelected = false;
            if (this.selected.Remove(shelveset))
            {
                this.OnSelectionChanged();
            }
        }

        /// <summary>
        /// Shows the page of the given index.
        /// </summary>
        /// <param name="index">The index of the page</param>
        private void ShowPage(int index)
        {
            this.pageIndex = Math.Max(0, index);
            this.CurrentPage = this.items.Skip(this.pageIndex * this.PageSize).Take(this.PageSize).ToList().AsReadOnly();

            this.OnPropertyChanged(nameof(this.CurrentPage));
            this.OnPropertyChanged(nameof(this.PageIndex));
            this.OnPropertyChanged(nameof(this.PageCount));
            this.OnPropertyChanged(nameof(this.PageText));
            this.OnPropertyChanged(nameof(this.CanGoPrevious));
            this.OnPropertyChanged(nameof(this.CanGoNext));
            this.OnPropertyChanged(nameof(this.Items));
            this.NextPageCommand.RaiseCanExecuteChanged();
            this.PreviousPageCommand.RaiseCanExecuteChanged();

            var withoutDetails = this.CurrentPage.Where(s => !s.AreDetailsLoaded).ToList();
            if (withoutDetails.Count > 0)
            {
                this.PageShown?.Invoke(this, new PageShownEventArgs(withoutDetails.AsReadOnly()));
            }
        }

        /// <summary>
        /// Sorts the shelvesets by the current sort property, keeping the order of the shelvesets that
        /// compare equal.
        /// </summary>
        private void ApplySort()
        {
            if (string.IsNullOrEmpty(this.SortPropertyName))
            {
                return;
            }

            var property = typeof(ShelvesetViewModel).GetProperty(this.SortPropertyName);
            if (property == null)
            {
                return;
            }

            var comparer = new SortValueComparer();
            this.items = (this.SortDirection == ListSortDirection.Ascending
                    ? this.items.OrderBy(s => property.GetValue(s), comparer)
                    : this.items.OrderByDescending(s => property.GetValue(s), comparer))
                .ToList();
        }

        /// <summary>
        /// Raises the notifications of a change of the selection.
        /// </summary>
        private void OnSelectionChanged()
        {
            this.OnPropertyChanged(nameof(this.SelectedShelvesets));
            this.OnPropertyChanged(nameof(this.SelectionText));
        }

        /// <summary>
        /// Raises the PropertyChanged event for the given property.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Compares the values of a sort column: text ignoring case, and other values such as dates by their
        /// own comparison rather than by their text.
        /// </summary>
        private class SortValueComparer : IComparer<object>
        {
            /// <inheritdoc/>
            public int Compare(object x, object y)
            {
                if (x is string || y is string)
                {
                    return StringComparer.CurrentCultureIgnoreCase.Compare(x as string ?? string.Empty, y as string ?? string.Empty);
                }

                return Comparer.Default.Compare(x, y);
            }
        }
    }

    /// <summary>
    /// The shelvesets of a page just shown whose details are not loaded yet.
    /// </summary>
    public class PageShownEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the PageShownEventArgs class.
        /// </summary>
        /// <param name="shelvesetsWithoutDetails">The shelvesets whose details are not loaded yet</param>
        public PageShownEventArgs(IReadOnlyList<ShelvesetViewModel> shelvesetsWithoutDetails)
        {
            this.ShelvesetsWithoutDetails = shelvesetsWithoutDetails;
        }

        /// <summary>
        /// Gets the shelvesets of the page whose details are not loaded yet.
        /// </summary>
        public IReadOnlyList<ShelvesetViewModel> ShelvesetsWithoutDetails { get; private set; }
    }
}
