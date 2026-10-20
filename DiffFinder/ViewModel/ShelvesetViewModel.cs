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

        public ShelvesetViewModel(string name, DateTime creationDate, string ownerDisplayName, string ownerName = null, string comment = null, IEnumerable<ShelvesetWorkItemViewModel> workItems = null)
        {
            Name = name;
            CreationDate = creationDate;
            OwnerDisplayName = ownerDisplayName;
            OwnerName = ownerName ?? ownerDisplayName;
            Comment = comment ?? string.Empty;
            WorkItems = (workItems ?? Enumerable.Empty<ShelvesetWorkItemViewModel>()).ToList().AsReadOnly();
        }
        public ShelvesetViewModel(Shelveset shelveset)
            : this(shelveset.Name, shelveset.CreationDate, shelveset.OwnerDisplayName, shelveset.OwnerName, shelveset.Comment, ReadWorkItems(shelveset))
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
        /// Gets the work items associated with the shelveset.
        /// </summary>
        public IReadOnlyList<ShelvesetWorkItemViewModel> WorkItems { get; private set; }

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
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsExpanded)));
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
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsSelected)));
            }
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
    }
}
