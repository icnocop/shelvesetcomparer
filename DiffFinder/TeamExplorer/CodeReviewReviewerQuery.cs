namespace DiffFinder
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.WorkItemTracking.Client;

    /// <summary>
    /// Reads the reviewers of the code reviews requested on shelvesets from the work items of the server: a
    /// code review is a Code Review Request work item naming the shelveset, with a Code Review Response
    /// child work item assigned to each reviewer.
    /// </summary>
    /// <remarks>
    /// The work item client object model is marked obsolete in favor of the REST client, which would bring
    /// in several more assemblies. It ships with Team Explorer, whose own code review pages still use it,
    /// so it is available wherever the extension runs.
    /// </remarks>
#pragma warning disable CS0618 // Type or member is obsolete
    public static class CodeReviewReviewerQuery
    {
        /// <summary>
        /// The field holding who a work item is assigned to
        /// </summary>
        private const string AssignedToField = "System.AssignedTo";

        /// <summary>
        /// Reads the code reviews requested on the given shelvesets, with a single query for all of them.
        /// </summary>
        /// <param name="collection">The team project collection</param>
        /// <param name="shelvesets">The shelvesets</param>
        /// <returns>The code reviews of the shelvesets</returns>
        public static CodeReviewIndex Read(TfsTeamProjectCollection collection, IEnumerable<ShelvesetViewModel> shelvesets)
        {
            var wiql = CodeReviewWiql.RequestsAndResponses(shelvesets.Select(s => s.Name));
            var store = collection?.GetService<WorkItemStore>();
            if (wiql == null || store == null)
            {
                return new CodeReviewIndex(null, null);
            }

            // the requests come back as links without a source, the responses as links from their request
            var links = new Query(store, wiql).RunLinkQuery();
            var ids = links.SelectMany(link => new[] { link.SourceId, link.TargetId }).Where(id => id > 0).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new CodeReviewIndex(null, null);
            }

            var workItems = store
                .Query(ids, "SELECT [System.Id], [" + AssignedToField + "], [" + CodeReviewWiql.ContextField + "], [" + CodeReviewWiql.ContextOwnerField + "] FROM WorkItems")
                .Cast<WorkItem>()
                .ToDictionary(workItem => workItem.Id);

            var requests = links
                .Where(link => link.SourceId == 0 && workItems.ContainsKey(link.TargetId))
                .Select(link => workItems[link.TargetId])
                .Select(request => new CodeReviewRequest(request.Id, ReadField(request, CodeReviewWiql.ContextField), ReadField(request, CodeReviewWiql.ContextOwnerField)))
                .ToList();

            var responses = links
                .Where(link => link.SourceId > 0 && workItems.ContainsKey(link.TargetId))
                .Select(link => new CodeReviewResponse(link.SourceId, ReadField(workItems[link.TargetId], AssignedToField)))
                .ToList();

            return new CodeReviewIndex(requests, responses);
        }

        /// <summary>
        /// Reads the value of a field of the work item as text.
        /// </summary>
        /// <param name="workItem">The work item</param>
        /// <param name="field">The reference name of the field</param>
        /// <returns>The value, or null when the work item has no such field</returns>
        private static string ReadField(WorkItem workItem, string field)
        {
            return workItem.Fields.Contains(field) ? workItem.Fields[field].Value as string : null;
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
