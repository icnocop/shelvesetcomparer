using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="CodeReviewWiql"/>, which writes the work item queries reading the code reviews of
    /// shelvesets.
    /// </summary>
    [TestClass]
    public class CodeReviewWiqlTests
    {
        [TestMethod]
        public void RequestsAndResponses_LinksTheRequestsOfTheShelvesetsToTheirResponses()
        {
            var query = CodeReviewWiql.RequestsAndResponses(new[] { "Fix the login", "Fix the logout" });

            StringAssert.Contains(query, "FROM WorkItemLinks");
            StringAssert.Contains(query, "[Source].[System.WorkItemType] IN GROUP 'Microsoft.CodeReviewRequestCategory'");
            StringAssert.Contains(query, "[Source].[Microsoft.VSTS.CodeReview.Context] IN ('Fix the login', 'Fix the logout')");
            StringAssert.Contains(query, "[System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward'");
            StringAssert.Contains(query, "[Target].[System.WorkItemType] IN GROUP 'Microsoft.CodeReviewResponseCategory'");
            StringAssert.Contains(query, "MODE (MayContain)");
        }

        [TestMethod]
        public void RequestsAndResponses_EscapesQuotesAndListsEachNameOnce()
        {
            var query = CodeReviewWiql.RequestsAndResponses(new[] { "John's fix", "john's FIX", null, " " });

            StringAssert.Contains(query, "IN ('John''s fix')");
        }

        [TestMethod]
        public void RequestsAndResponses_NoShelveset_IsNull()
        {
            Assert.IsNull(CodeReviewWiql.RequestsAndResponses(new string[0]));
            Assert.IsNull(CodeReviewWiql.RequestsAndResponses(null));
        }
    }
}
