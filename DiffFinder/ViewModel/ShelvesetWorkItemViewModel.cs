// <copyright file="ShelvesetWorkItemViewModel.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

namespace DiffFinder
{
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// A work item associated with a shelveset, listed under the shelveset so that the shelvesets belonging
    /// to the same work item can be told apart from the unrelated ones shelved in between.
    /// </summary>
    public class ShelvesetWorkItemViewModel
    {
        /// <summary>
        /// Initializes a new instance of the ShelvesetWorkItemViewModel class.
        /// </summary>
        /// <param name="id">The id of the work item</param>
        /// <param name="title">The title of the work item, if known</param>
        /// <param name="workItemType">The type of the work item, if known</param>
        /// <param name="state">The state of the work item, if known</param>
        /// <param name="assignedTo">The user the work item is assigned to, if known</param>
        public ShelvesetWorkItemViewModel(int id, string title = null, string workItemType = null, string state = null, string assignedTo = null)
        {
            this.Id = id;
            this.Title = title ?? string.Empty;
            this.WorkItemType = workItemType ?? string.Empty;
            this.State = state ?? string.Empty;
            this.AssignedTo = assignedTo ?? string.Empty;
        }

        /// <summary>
        /// Gets the id of the work item.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the title of the work item, empty when the server did not provide it.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the type of the work item, empty when the server did not provide it.
        /// </summary>
        public string WorkItemType { get; private set; }

        /// <summary>
        /// Gets the state of the work item, empty when the server did not provide it.
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Gets the user the work item is assigned to, empty when the server did not provide it.
        /// </summary>
        public string AssignedTo { get; private set; }

        /// <summary>
        /// Gets the line the work item is listed with, such as "Task 123: Fix the login (Active)". The parts
        /// the server did not provide are left out.
        /// </summary>
        public string DisplayText
        {
            get
            {
                var text = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(this.WorkItemType))
                {
                    text.Append(this.WorkItemType).Append(' ');
                }

                text.Append(this.Id.ToString(CultureInfo.CurrentCulture));

                if (!string.IsNullOrWhiteSpace(this.Title))
                {
                    text.Append(": ").Append(this.Title);
                }

                if (!string.IsNullOrWhiteSpace(this.State))
                {
                    text.Append(" (").Append(this.State).Append(')');
                }

                return text.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.DisplayText;
        }
    }
}
