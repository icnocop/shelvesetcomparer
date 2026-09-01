// <copyright file="ShelvesetsContext.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

using System.Collections.ObjectModel;

namespace DiffFinder
{
    /// <summary>
    /// The class provides the place holder for storing shelveset information in the Shelveset Comparer team explorer window.
    /// Team Explorer hands an instance back through SectionInitializeEventArgs.Context on some navigations
    /// only, so the last instance saved is also kept in <see cref="Current"/> and used when it does not.
    /// </summary>
    internal class ShelvesetsContext
    {
        /// <summary>
        /// Gets or sets the state of the section as it was left the last time it was shown. Restoring it is
        /// what lets navigating away to a shelveset and back keep the listed shelvesets and the selection.
        /// </summary>
        public static ShelvesetsContext Current { get; set; }

        /// <summary>
        /// Gets or sets the team project collection and team project the state belongs to. Restoring state
        /// captured against a different team project would list shelvesets that are not the current ones.
        /// </summary>
        public string ContextKey { get; set; }

        /// <summary>
        /// Gets or sets the list of Shelveset.
        /// </summary>
        public ObservableCollection<ShelvesetViewModel> Shelvesets { get; set; }

        /// <summary>
        /// Gets or sets the list of users offered by the shelveset owner drop down lists.
        /// </summary>
        public ObservableCollection<string> Users { get; set; }

        /// <summary>
        /// Gets or sets the user account name selected for the first shelveset.
        /// </summary>
        public string FirstUserAccountName { get; set; }

        /// <summary>
        /// Gets or sets the user account name selected for the second shelveset.
        /// </summary>
        public string SecondUserAccountName { get; set; }
    }
}
