namespace DiffFinder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A code review requested on a shelveset: a Code Review Request work item, which names the shelveset in
    /// its context fields.
    /// </summary>
    public class CodeReviewRequest
    {
        /// <summary>
        /// Initializes a new instance of the CodeReviewRequest class.
        /// </summary>
        /// <param name="id">The id of the Code Review Request work item</param>
        /// <param name="shelvesetName">The name of the shelveset the review was requested on</param>
        /// <param name="shelvesetOwner">The owner of the shelveset the review was requested on</param>
        public CodeReviewRequest(int id, string shelvesetName, string shelvesetOwner)
        {
            this.Id = id;
            this.ShelvesetName = shelvesetName ?? string.Empty;
            this.ShelvesetOwner = shelvesetOwner ?? string.Empty;
        }

        /// <summary>
        /// Gets the id of the Code Review Request work item.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the name of the shelveset the review was requested on.
        /// </summary>
        public string ShelvesetName { get; private set; }

        /// <summary>
        /// Gets the owner of the shelveset the review was requested on.
        /// </summary>
        public string ShelvesetOwner { get; private set; }
    }

    /// <summary>
    /// A reviewer asked to review a code review: a Code Review Response work item, a child of the request,
    /// assigned to the reviewer.
    /// </summary>
    public class CodeReviewResponse
    {
        /// <summary>
        /// Initializes a new instance of the CodeReviewResponse class.
        /// </summary>
        /// <param name="requestId">The id of the Code Review Request work item the response belongs to</param>
        /// <param name="reviewer">The reviewer the response is assigned to</param>
        public CodeReviewResponse(int requestId, string reviewer)
        {
            this.RequestId = requestId;
            this.Reviewer = reviewer ?? string.Empty;
        }

        /// <summary>
        /// Gets the id of the Code Review Request work item the response belongs to.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// Gets the reviewer the response is assigned to.
        /// </summary>
        public string Reviewer { get; private set; }
    }

    /// <summary>
    /// Finds the reviewers of the code reviews requested on a shelveset. Kept free of the work item tracking
    /// types so that it can be exercised without a server.
    /// </summary>
    public class CodeReviewIndex
    {
        /// <summary>
        /// The code review requests, by shelveset name
        /// </summary>
        private readonly ILookup<string, CodeReviewRequest> requestsByShelveset;

        /// <summary>
        /// The reviewers, by the id of the code review request
        /// </summary>
        private readonly ILookup<int, string> reviewersByRequest;

        /// <summary>
        /// Initializes a new instance of the CodeReviewIndex class.
        /// </summary>
        /// <param name="requests">The code review requests</param>
        /// <param name="responses">The code review responses</param>
        public CodeReviewIndex(IEnumerable<CodeReviewRequest> requests, IEnumerable<CodeReviewResponse> responses)
        {
            this.requestsByShelveset = (requests ?? Enumerable.Empty<CodeReviewRequest>())
                .Where(request => request != null)
                .ToLookup(request => request.ShelvesetName, StringComparer.OrdinalIgnoreCase);
            this.reviewersByRequest = (responses ?? Enumerable.Empty<CodeReviewResponse>())
                .Where(response => response != null && !string.IsNullOrWhiteSpace(response.Reviewer))
                .ToLookup(response => response.RequestId, response => response.Reviewer.Trim());
        }

        /// <summary>
        /// Returns the reviewers of the code reviews requested on the given shelveset.
        /// </summary>
        /// <param name="shelvesetName">The name of the shelveset</param>
        /// <param name="ownerNames">The names the owner of the shelveset may be recorded by, such as the
        /// account name and the display name</param>
        /// <returns>The reviewers, each listed once, sorted</returns>
        public IReadOnlyList<string> GetReviewers(string shelvesetName, params string[] ownerNames)
        {
            var owners = new HashSet<string>((ownerNames ?? new string[0]).Where(name => !string.IsNullOrWhiteSpace(name)), StringComparer.OrdinalIgnoreCase);

            return this.requestsByShelveset[shelvesetName ?? string.Empty]
                .Where(request => owners.Contains(request.ShelvesetOwner))
                .SelectMany(request => this.reviewersByRequest[request.Id])
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(reviewer => reviewer, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
                .AsReadOnly();
        }
    }
}
