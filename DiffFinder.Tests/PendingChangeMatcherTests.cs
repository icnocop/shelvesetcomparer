using System.IO;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="PendingChangeMatcher"/>. The Team Foundation PendingChange is sealed, which is
    /// why the code under test goes through the <see cref="IPendingChange"/> facade; these tests mock that
    /// facade rather than talking to a server.
    /// </summary>
    [TestClass]
    public class PendingChangeMatcherTests
    {
        [TestMethod]
        public void FindMatch_SameItemId_MatchesEvenWhenThePathDiffers()
        {
            var change = Change(itemId: 1, item: "$/Main/BranchA/src/file1");
            var other = Change(itemId: 1, item: "$/Main/BranchB/src/renamed");

            var match = PendingChangeMatcher.FindMatch(change, new[] { Change(itemId: 9, item: "$/x/y"), other });

            Assert.AreSame(other, match);
        }

        [TestMethod]
        public void FindMatch_SamePath_MatchesWhenTheItemIdDiffers()
        {
            var change = Change(itemId: 1, item: "$/Main/BranchA/src/file1");
            var other = Change(itemId: 77, item: "$/Main/BranchA/src/file1");

            var match = PendingChangeMatcher.FindMatch(change, new[] { other });

            Assert.AreSame(other, match);
        }

        [TestMethod]
        public void FindMatch_DifferentBranch_MatchesOnTheRelativePath()
        {
            var change = Change(itemId: 1, item: "$/Main/BranchA/src/file1");
            var other = Change(itemId: 10, item: "$/Main/BranchB/src/file1");

            var match = PendingChangeMatcher.FindMatch(change, new[] { other });

            Assert.AreSame(other, match);
        }

        [TestMethod]
        public void FindMatch_NoCounterpart_ReturnsNull()
        {
            var change = Change(itemId: 1, item: "$/Main/BranchA/src/file1");
            var other = Change(itemId: 2, item: "$/Main/BranchB/include/somethingelse");

            Assert.IsNull(PendingChangeMatcher.FindMatch(change, new[] { other }));
        }

        [TestMethod]
        public void FindMatch_NoOtherChanges_ReturnsNull()
        {
            var change = Change(itemId: 1, item: "$/Main/BranchA/src/file1");

            Assert.IsNull(PendingChangeMatcher.FindMatch(change, new IPendingChange[0]));
        }

        [TestMethod]
        public void FindMatch_NullArguments_ReturnNull()
        {
            Assert.IsNull(PendingChangeMatcher.FindMatch(null, new IPendingChange[0]));
            Assert.IsNull(PendingChangeMatcher.FindMatch(Change(1, "$/a/b"), null));
        }

        [TestMethod]
        public void AreContentsSame_EqualUploadHashes_ReturnsTrueWithoutDownloading()
        {
            var first = ChangeMock(1, "$/a/file", hash: new byte[] { 0x1, 0x2 });
            var second = ChangeMock(2, "$/b/file", hash: new byte[] { 0x1, 0x2 });

            Assert.IsTrue(PendingChangeMatcher.AreContentsSame(first.Object, second.Object));

            first.Verify(c => c.DownloadShelvedFile(), Times.Never);
            second.Verify(c => c.DownloadShelvedFile(), Times.Never);
        }

        [TestMethod]
        public void AreContentsSame_DifferingUploadHashes_ReturnsFalse()
        {
            var first = Change(1, "$/a/file", hash: new byte[] { 0x1, 0x2 });
            var second = Change(2, "$/b/file", hash: new byte[] { 0x1, 0x3 });

            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(first, second));
        }

        [TestMethod]
        public void AreContentsSame_NoUploadHash_ComparesTheDownloadedContent()
        {
            var first = Change(1, "$/a/file", content: "the same bytes");
            var second = Change(2, "$/b/file", content: "the same bytes");

            Assert.IsTrue(PendingChangeMatcher.AreContentsSame(first, second));
        }

        [TestMethod]
        public void AreContentsSame_NoUploadHashAndDifferingContent_ReturnsFalse()
        {
            var first = Change(1, "$/a/file", content: "one thing");
            var second = Change(2, "$/b/file", content: "another thing");

            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(first, second));
        }

        [TestMethod]
        public void AreContentsSame_ContentOfDifferentLengthWithACommonPrefix_ReturnsFalse()
        {
            var first = Change(1, "$/a/file", content: "abc");
            var second = Change(2, "$/b/file", content: "abcdef");

            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(first, second));
        }

        [TestMethod]
        public void AreContentsSame_DeletedFile_ReturnsFalse()
        {
            var first = Change(1, "$/a/file", hash: new byte[] { 0x1 }, changeType: ChangeType.Delete);
            var second = Change(2, "$/b/file", hash: new byte[] { 0x1 });

            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(first, second));
            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(second, first));
        }

        [TestMethod]
        public void AreContentsSame_NullArguments_ReturnFalse()
        {
            var change = Change(1, "$/a/file", hash: new byte[] { 0x1 });

            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(null, change));
            Assert.IsFalse(PendingChangeMatcher.AreContentsSame(change, null));
        }

        /// <summary>
        /// Creates a mocked pending change. Only the members the matcher reads are set up, so an unexpected
        /// dependency on any other member shows up as a failing test rather than as silent default values.
        /// </summary>
        private static IPendingChange Change(int itemId, string item, byte[] hash = null, string content = null, ChangeType changeType = ChangeType.Edit)
        {
            return ChangeMock(itemId, item, hash, content, changeType).Object;
        }

        /// <summary>
        /// Creates a mocked pending change and returns the mock itself, for the tests that assert on which
        /// members the matcher used.
        /// </summary>
        private static Mock<IPendingChange> ChangeMock(int itemId, string item, byte[] hash = null, string content = null, ChangeType changeType = ChangeType.Edit)
        {
            var mock = new Mock<IPendingChange>(MockBehavior.Strict);
            mock.SetupGet(c => c.ItemId).Returns(itemId);
            mock.SetupGet(c => c.LocalOrServerItem).Returns(item);
            mock.SetupGet(c => c.ChangeType).Returns(changeType);
            mock.SetupGet(c => c.UploadHashValue).Returns(hash);

            if (content != null)
            {
                mock.Setup(c => c.DownloadShelvedFile()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
            }
            else
            {
                mock.Setup(c => c.DownloadShelvedFile()).Returns(() => new MemoryStream());
            }

            return mock;
        }
    }
}
