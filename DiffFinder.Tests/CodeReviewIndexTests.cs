using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="CodeReviewIndex"/>, which finds the reviewers of the code reviews requested on a
    /// shelveset.
    /// </summary>
    [TestClass]
    public class CodeReviewIndexTests
    {
        private static readonly CodeReviewRequest[] Requests =
        {
            new CodeReviewRequest(1, "Fix the login", @"CONTOSO\jsmith"),
            new CodeReviewRequest(2, "Fix the logout", @"CONTOSO\jsmith"),
            new CodeReviewRequest(3, "Fix the login", @"CONTOSO\other"),
            new CodeReviewRequest(4, "Fix the login", @"CONTOSO\jsmith")
        };

        private static readonly CodeReviewResponse[] Responses =
        {
            new CodeReviewResponse(1, "Bob Jones"),
            new CodeReviewResponse(1, "Ann Lee"),
            new CodeReviewResponse(1, "ann lee"),
            new CodeReviewResponse(1, " "),
            new CodeReviewResponse(2, "Carl Diaz"),
            new CodeReviewResponse(3, "Dana Fox"),
            new CodeReviewResponse(4, "Eve Ross")
        };

        [TestMethod]
        public void GetReviewers_ListsTheReviewersOfTheShelvesetOnce_Sorted()
        {
            var index = new CodeReviewIndex(Requests, Responses);

            CollectionAssert.AreEqual(
                new[] { "Ann Lee", "Bob Jones", "Eve Ross" },
                index.GetReviewers("Fix the login", @"CONTOSO\jsmith").ToArray());
        }

        [TestMethod]
        public void GetReviewers_IgnoresCasing()
        {
            var index = new CodeReviewIndex(Requests, Responses);

            CollectionAssert.AreEqual(new[] { "Carl Diaz" }, index.GetReviewers("FIX THE LOGOUT", @"contoso\JSMITH").ToArray());
        }

        [TestMethod]
        public void GetReviewers_SameShelvesetNameOfAnotherOwner_IsNotMixedIn()
        {
            var index = new CodeReviewIndex(Requests, Responses);

            CollectionAssert.AreEqual(new[] { "Dana Fox" }, index.GetReviewers("Fix the login", @"CONTOSO\other").ToArray());
        }

        [TestMethod]
        public void GetReviewers_MatchesAnyOfTheNamesOfTheOwner()
        {
            // the owner may be recorded by account name or by display name
            var index = new CodeReviewIndex(new[] { new CodeReviewRequest(9, "review", "John Smith") }, new[] { new CodeReviewResponse(9, "Ann Lee") });

            CollectionAssert.AreEqual(new[] { "Ann Lee" }, index.GetReviewers("review", @"CONTOSO\jsmith", "John Smith").ToArray());
        }

        [TestMethod]
        public void GetReviewers_NoCodeReview_IsEmpty()
        {
            var index = new CodeReviewIndex(Requests, Responses);

            Assert.AreEqual(0, index.GetReviewers("Not reviewed", @"CONTOSO\jsmith").Count);
        }

        [TestMethod]
        public void GetReviewers_NothingIndexed_IsEmpty()
        {
            Assert.AreEqual(0, new CodeReviewIndex(null, null).GetReviewers("review", "owner").Count);
        }
    }
}
