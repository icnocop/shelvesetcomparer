namespace DiffFinder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Writes the work item queries reading the code reviews requested on shelvesets. Kept apart from the
    /// code running them so that it can be exercised without the work item tracking assemblies.
    /// </summary>
    public static class CodeReviewWiql
    {
        /// <summary>
        /// The field of a Code Review Request naming the shelveset the review was requested on
        /// </summary>
        public const string ContextField = "Microsoft.VSTS.CodeReview.Context";

        /// <summary>
        /// The field of a Code Review Request naming the owner of the shelveset the review was requested on
        /// </summary>
        public const string ContextOwnerField = "Microsoft.VSTS.CodeReview.ContextOwner";

        /// <summary>
        /// Writes the query linking the code review requests on the given shelvesets to their responses,
        /// one per reviewer. The requests without any response are returned too.
        /// </summary>
        /// <param name="shelvesetNames">The names of the shelvesets</param>
        /// <returns>The query, or null when there is no shelveset to query</returns>
        public static string RequestsAndResponses(IEnumerable<string> shelvesetNames)
        {
            var names = (shelvesetNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => "'" + name.Replace("'", "''") + "'")
                .ToList();

            if (names.Count == 0)
            {
                return null;
            }

            return "SELECT [System.Id] FROM WorkItemLinks"
                + " WHERE ([Source].[System.WorkItemType] IN GROUP 'Microsoft.CodeReviewRequestCategory'"
                + " AND [Source].[" + ContextField + "] IN (" + string.Join(", ", names) + "))"
                + " AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward')"
                + " AND ([Target].[System.WorkItemType] IN GROUP 'Microsoft.CodeReviewResponseCategory')"
                + " MODE (MayContain)";
        }
    }
}
