// <copyright file="ShelvesetViewModel.cs" company="http://shelvesetcomparer.codeplex.com">
// Copyright http://shelvesetcomparer.codeplex.com. All Rights Reserved.
// This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html).
// This is sample code only, do not use in production environments.
// </copyright>

namespace DiffFinder
{
    using Microsoft.TeamFoundation.VersionControl.Client;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Helper class to abstract from Microsoft Shelvset (to allow test values for debugging)
    /// </summary>
    public class ShelvesetViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Whether the shelveset is selected in the list
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Whether the work items of the shelveset are listed under it
        /// </summary>
        private bool isExpanded;

        /// <summary>
        /// The reviewers of the code reviews requested on the shelveset
        /// </summary>
        private IReadOnlyList<string> reviewers = new string[0];

        /// <summary>
        /// Whether the signed in user is one of the reviewers
        /// </summary>
        private bool isAssignedToMe;

        /// <summary>
        /// Reads the work items of the shelveset, null once they are read
        /// </summary>
        private Func<IEnumerable<ShelvesetWorkItemViewModel>> workItemsLoader;

        /// <summary>
        /// The work items associated with the shelveset
        /// </summary>
        private IReadOnlyList<ShelvesetWorkItemViewModel> workItems;

        /// <summary>
        /// Initializes a new instance of the ShelvesetViewModel class.
        /// </summary>
        /// <param name="name">The name of the shelveset</param>
        /// <param name="creationDate">When the shelveset was created</param>
        /// <param name="ownerDisplayName">The display name of the owner</param>
        /// <param name="ownerName">The account name of the owner, the display name when not given</param>
        /// <param name="comment">The comment the shelveset was shelved with</param>
        /// <param name="workItems">The work items associated with the shelveset, when already known</param>
        /// <param name="workItemsLoader">Reads the work items associated with the shelveset, called by
        /// <see cref="LoadDetails"/> rather than up front so that only the shelvesets listed pay for it</param>
        public ShelvesetViewModel(string name, DateTime creationDate, string ownerDisplayName, string ownerName = null, string comment = null, IEnumerable<ShelvesetWorkItemViewModel> workItems = null, Func<IEnumerable<ShelvesetWorkItemViewModel>> workItemsLoader = null)
        {
            Name = name;
            CreationDate = creationDate;
            OwnerDisplayName = ownerDisplayName;
            OwnerName = ownerName ?? ownerDisplayName;
            Comment = comment ?? string.Empty;
            this.workItems = (workItems ?? Enumerable.Empty<ShelvesetWorkItemViewModel>()).ToList().AsReadOnly();
            this.workItemsLoader = workItems == null ? workItemsLoader : null;
        }

        public ShelvesetViewModel(Shelveset shelveset)
            : this(shelveset.Name, shelveset.CreationDate, shelveset.OwnerDisplayName, shelveset.OwnerName, shelveset.Comment, workItemsLoader: () => ReadWorkItems(shelveset))
        {
            Shelveset = shelveset;
        }

        /// <summary>
        /// Notification event used by the list to update itself when the selection changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public DateTime CreationDate { get; set; }
        public string OwnerDisplayName { get; set; }
        public string OwnerName { get; set; }
        public Shelveset Shelveset { get; set; }

        /// <summary>
        /// Gets the comment the shelveset was shelved with.
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Gets the first non blank line of the comment, which is what fits in the comment column. The full
        /// comment is in the tooltip of the row.
        /// </summary>
        public string CommentFirstLine
        {
            get
            {
                return GetFirstLine(this.Comment);
            }
        }

        /// <summary>
        /// Gets the work items associated with the shelveset. Empty until the details are loaded.
        /// </summary>
        public IReadOnlyList<ShelvesetWorkItemViewModel> WorkItems
        {
            get
            {
                return this.workItems;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the details of the shelveset, its work items, are loaded.
        /// </summary>
        public bool AreDetailsLoaded
        {
            get
            {
                return this.workItemsLoader == null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any work item is associated with the shelveset, which is when the
        /// list offers to expand it.
        /// </summary>
        public bool HasWorkItems
        {
            get
            {
                return this.WorkItems.Count > 0;
            }
        }

        /// <summary>
        /// Gets or sets the reviewers of the code reviews requested on the shelveset. Empty until they are
        /// read, and for the shelvesets no review was requested on.
        /// </summary>
        public IReadOnlyList<string> Reviewers
        {
            get
            {
                return this.reviewers;
            }

            set
            {
                this.reviewers = (value ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
                this.OnPropertyChanged(nameof(this.Reviewers));
                this.OnPropertyChanged(nameof(this.ReviewersText));
            }
        }

        /// <summary>
        /// Gets the reviewers as listed in the reviewers column, such as "Ann Lee, Bob Jones".
        /// </summary>
        public string ReviewersText
        {
            get
            {
                return string.Join(", ", this.reviewers);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the signed in user is one of the reviewers, which is when
        /// the list highlights the shelveset.
        /// </summary>
        public bool IsAssignedToMe
        {
            get
            {
                return this.isAssignedToMe;
            }

            set
            {
                if (this.isAssignedToMe == value)
                {
                    return;
                }

                this.isAssignedToMe = value;
                this.OnPropertyChanged(nameof(this.IsAssignedToMe));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the work items of the shelveset are listed under it. Held
        /// on the item for the same reason as <see cref="IsSelected"/>.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                if (this.isExpanded == value)
                {
                    return;
                }

                this.isExpanded = value;
                this.OnPropertyChanged(nameof(this.IsExpanded));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the shelveset is selected in the list. Held on the item
        /// rather than in the list control so that the selection survives navigating away from the section
        /// and back, which restores the same items.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                if (this.isSelected == value)
                {
                    return;
                }

                this.isSelected = value;
                this.OnPropertyChanged(nameof(this.IsSelected));
            }
        }

        /// <summary>
        /// Loads the details of the shelveset, its work items, unless they are loaded already. A failure
        /// only costs the work items, it does not keep the shelveset from being listed.
        /// </summary>
        public void LoadDetails()
        {
            var loader = this.workItemsLoader;
            if (loader == null)
            {
                return;
            }

            IEnumerable<ShelvesetWorkItemViewModel> loaded;
            try
            {
                loaded = loader();
            }
            catch (Exception)
            {
                loaded = null;
            }

            this.workItems = (loaded ?? Enumerable.Empty<ShelvesetWorkItemViewModel>()).ToList().AsReadOnly();
            this.workItemsLoader = null;
            this.OnPropertyChanged(nameof(this.WorkItems));
            this.OnPropertyChanged(nameof(this.HasWorkItems));
            this.OnPropertyChanged(nameof(this.AreDetailsLoaded));
        }

        /// <summary>
        /// Tells whether the given user is one of the reviewers.
        /// </summary>
        /// <param name="user">The display name of the user</param>
        /// <returns>True when the user is a reviewer</returns>
        public bool IsReviewer(string user)
        {
            return !string.IsNullOrWhiteSpace(user)
                && this.reviewers.Any(reviewer => string.Equals(reviewer, user.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Returns the first non blank line of the given text, trimmed.
        /// </summary>
        /// <param name="text">The text, possibly spanning several lines</param>
        /// <returns>The first non blank line, or an empty string when there is none</returns>
        public static string GetFirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0) ?? string.Empty;
        }

        /// <summary>
        /// Combines the work items the server described with the ids of the ones it only listed, so that a
        /// work item is shown even when the server did not describe it.
        /// </summary>
        /// <param name="describedWorkItems">The work items the server described</param>
        /// <param name="workItemIds">The ids of the work items associated with the shelveset</param>
        /// <returns>The work items, each listed once, in the order the server returned them</returns>
        public static IReadOnlyList<ShelvesetWorkItemViewModel> MergeWorkItems(IEnumerable<ShelvesetWorkItemViewModel> describedWorkItems, IEnumerable<int> workItemIds)
        {
            var workItems = new List<ShelvesetWorkItemViewModel>();
            var listed = new HashSet<int>();

            foreach (var workItem in describedWorkItems ?? Enumerable.Empty<ShelvesetWorkItemViewModel>())
            {
                if (workItem != null && listed.Add(workItem.Id))
                {
                    workItems.Add(workItem);
                }
            }

            foreach (var id in workItemIds ?? Enumerable.Empty<int>())
            {
                if (listed.Add(id))
                {
                    workItems.Add(new ShelvesetWorkItemViewModel(id));
                }
            }

            return workItems.AsReadOnly();
        }

        /// <summary>
        /// Reads the work items associated with the shelveset. A failure only costs the work items, it does
        /// not keep the shelveset from being listed.
        /// </summary>
        /// <param name="shelveset">The shelveset</param>
        /// <returns>The work items associated with the shelveset</returns>
        private static IReadOnlyList<ShelvesetWorkItemViewModel> ReadWorkItems(Shelveset shelveset)
        {
            IEnumerable<ShelvesetWorkItemViewModel> described = null;
            IEnumerable<int> ids = null;

            try
            {
                described = shelveset.AssociatedWorkItems?
                    .Where(info => info != null)
                    .Select(info => new ShelvesetWorkItemViewModel(info.Id, info.Title, info.WorkItemType, info.State, info.AssignedTo))
                    .ToList();
            }
            catch (Exception)
            {
                // older servers do not describe the work items, the ids below still identify them
            }

            try
            {
                ids = shelveset.BriefWorkItemInfo?
                    .Where(info => info != null)
                    .Select(info => info.Id)
                    .ToList();
            }
            catch (Exception)
            {
                // nothing more to list
            }

            return MergeWorkItems(described, ids);
        }

        /// <summary>
        /// Raises the PropertyChanged event for the given property.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
