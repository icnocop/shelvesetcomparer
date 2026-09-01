// <copyright file="ShelvesetViewModel.cs" company="http://shelvesetcomparer.codeplex.com">
// Copyright http://shelvesetcomparer.codeplex.com. All Rights Reserved. 
// This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html).
// This is sample code only, do not use in production environments.
// </copyright>

namespace DiffFinder
{
    using Microsoft.TeamFoundation.VersionControl.Client;
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Helper class to abstract from Microsoft Shelvset (to allow test values for debugging)
    /// </summary>
    public class ShelvesetViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Whether the shelveset is selected in the list
        /// </summary>
        private bool isSelected;

        public ShelvesetViewModel(string name, DateTime creationDate, string ownerDisplayName, string ownerName = null)
        {
            Name = name;
            CreationDate = creationDate;
            OwnerDisplayName = ownerDisplayName;
            OwnerName = ownerName ?? ownerDisplayName;
        }
        public ShelvesetViewModel(Shelveset shelveset)
            : this(shelveset.Name, shelveset.CreationDate, shelveset.OwnerDisplayName, shelveset.OwnerName)
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
    }
}
