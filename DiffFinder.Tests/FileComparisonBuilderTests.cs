using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="FileComparisonBuilder"/>, which pairs up the files of two shelvesets into the rows
    /// of the comparison summary and orders them.
    /// </summary>
    [TestClass]
    public class FileComparisonBuilderTests
    {
        [TestMethod]
        public void Build_PairsTheFilesAndCountsThem()
        {
            var same1 = Change(1, "$/P/src/Same.cs", hash: 1);
            var same2 = Change(1, "$/P/src/Same.cs", hash: 1);
            var changed1 = Change(2, "$/P/src/Changed.cs", hash: 1);
            var changed2 = Change(2, "$/P/src/Changed.cs", hash: 2);
            var onlyFirst = Change(3, "$/P/src/OnlyFirst.cs", hash: 1);
            var onlySecond = Change(4, "$/P/src/OnlySecond.cs", hash: 1);

            var comparison = FileComparisonBuilder.Build(new[] { same1, changed1, onlyFirst }, new[] { same2, changed2, onlySecond }, "review 1", "review 2");

            Assert.AreEqual(4, comparison.Files.Count);
            Assert.AreEqual(2, comparison.CommonFileCount);
            Assert.AreEqual(1, comparison.MatchingFileCount);
            Assert.AreEqual(3, comparison.DifferentFileCount);

            var same = comparison.Files.Single(f => f.FirstFile == same1);
            Assert.AreSame(same2, same.SecondFile);
            Assert.AreEqual(FileComparisonStatus.Matching, same.Status);
            Assert.AreEqual("review 1", same.FirstShelveName);
            Assert.AreEqual("review 2", same.SecondShelveName);
            Assert.AreEqual(FileComparisonStatus.Different, comparison.Files.Single(f => f.FirstFile == changed1).Status);
            Assert.AreEqual(FileComparisonStatus.NoMatch, comparison.Files.Single(f => f.FirstFile == onlyFirst).Status);
            Assert.AreEqual(FileComparisonStatus.NoMatch, comparison.Files.Single(f => f.SecondFile == onlySecond).Status);
        }

        [TestMethod]
        public void Build_FileRenamedInTheSecondShelveset_IsListedOnce()
        {
            var original = Change(7, "$/P/src/Old.cs", hash: 1);
            var renamed = Change(7, "$/P/src/New.cs", hash: 1);

            var comparison = FileComparisonBuilder.Build(new[] { original }, new[] { renamed }, "1", "2");

            Assert.AreEqual(1, comparison.Files.Count);
            Assert.AreSame(renamed, comparison.Files[0].SecondFile);
        }

        [TestMethod]
        public void Build_FileDeletedInBoth_IsMatching()
        {
            var first = Change(5, "$/P/src/Gone.cs", changeType: ChangeType.Delete);
            var second = Change(5, "$/P/src/Gone.cs", changeType: ChangeType.Delete);

            var comparison = FileComparisonBuilder.Build(new[] { first }, new[] { second }, "1", "2");

            Assert.AreEqual(FileComparisonStatus.Matching, comparison.Files.Single().Status);
            Assert.AreEqual(1, comparison.MatchingFileCount);
            Assert.AreEqual(0, comparison.DifferentFileCount);
        }

        [TestMethod]
        public void Build_FileDeletedInOneOnly_IsDifferent()
        {
            var first = Change(5, "$/P/src/Gone.cs", changeType: ChangeType.Delete);
            var second = Change(5, "$/P/src/Gone.cs", hash: 1);

            var comparison = FileComparisonBuilder.Build(new[] { first }, new[] { second }, "1", "2");

            Assert.AreEqual(FileComparisonStatus.Different, comparison.Files.Single().Status);
        }

        [TestMethod]
        public void Order_Tree_ListsTheSubfoldersOfAFolderBeforeItsFiles()
        {
            var rows = Rows("$/P/src/b.cs", "$/P/src/c/x.cs", "$/P/README.md", "$/P/src/a.cs", "$/P/src/c/d/y.cs");

            CollectionAssert.AreEqual(
                new[] { "$/P/src/c/d/y.cs", "$/P/src/c/x.cs", "$/P/src/a.cs", "$/P/src/b.cs", "$/P/README.md" },
                Paths(FileComparisonBuilder.Order(rows, FileOrder.Tree)));
        }

        [TestMethod]
        public void Order_Path_SortsByTheFullServerPath()
        {
            var rows = Rows("$/P/src/b.cs", "$/P/src/c/x.cs", "$/P/README.md", "$/P/src/a.cs");

            CollectionAssert.AreEqual(
                new[] { "$/P/README.md", "$/P/src/a.cs", "$/P/src/b.cs", "$/P/src/c/x.cs" },
                Paths(FileComparisonBuilder.Order(rows, FileOrder.Path)));
        }

        [TestMethod]
        public void Order_IgnoresCasingAndUsesOrdinalComparison()
        {
            // character by character ignoring case, where '.' < 'B' < 'C' < '_', whereas a culture aware
            // comparison gives punctuation its own weights
            var rows = Rows("$/P/src/b.cs", "$/P/src/A.cs", "$/P/src/a_b.cs", "$/P/src/a.b.cs");

            CollectionAssert.AreEqual(
                new[] { "$/P/src/a.b.cs", "$/P/src/A.cs", "$/P/src/a_b.cs", "$/P/src/b.cs" },
                Paths(FileComparisonBuilder.Order(rows, FileOrder.Path)));
        }

        [TestMethod]
        public void Order_UsesTheServerPathEvenWhenTheFileIsListedByItsLocalPath()
        {
            var local = Row(Change(1, @"C:\Work\src\z.cs", serverItem: "$/P/src/a.cs"));
            var server = Row(Change(2, "$/P/src/m.cs"));

            CollectionAssert.AreEqual(
                new[] { local, server },
                FileComparisonBuilder.Order(new[] { server, local }, FileOrder.Path).ToArray());
        }

        [TestMethod]
        public void Order_Shelveset_KeepsTheOrderTheFilesWereShelvedIn()
        {
            var first = new[] { Change(1, "$/P/z.cs"), Change(2, "$/P/a.cs") };
            var second = new[] { Change(3, "$/P/y.cs"), Change(1, "$/P/z.cs"), Change(4, "$/P/b.cs") };

            var comparison = FileComparisonBuilder.Build(first, second, "1", "2");

            CollectionAssert.AreEqual(
                new[] { "$/P/z.cs", "$/P/a.cs", "$/P/y.cs", "$/P/b.cs" },
                Paths(FileComparisonBuilder.Order(comparison.Files, FileOrder.Shelveset)));
        }

        [TestMethod]
        public void Order_RowOnlyInTheSecondShelveset_IsOrderedByItsPath()
        {
            var rows = new[] { Row(Change(1, "$/P/c.cs")), new FileComparisonViewModel { SecondFile = Change(2, "$/P/a.cs") } };

            CollectionAssert.AreEqual(new[] { "$/P/a.cs", "$/P/c.cs" }, Paths(FileComparisonBuilder.Order(rows, FileOrder.Tree)));
        }

        private static FileComparisonViewModel[] Rows(params string[] paths)
        {
            return paths.Select((path, index) => Row(Change(index + 1, path))).ToArray();
        }

        private static FileComparisonViewModel Row(IPendingChange file)
        {
            return new FileComparisonViewModel { FirstFile = file };
        }

        private static string[] Paths(System.Collections.Generic.IEnumerable<FileComparisonViewModel> rows)
        {
            return rows.Select(r => (r.FirstFile ?? r.SecondFile).ServerItem).ToArray();
        }

        private static IPendingChange Change(int itemId, string item, byte hash = 0, ChangeType changeType = ChangeType.Edit, string serverItem = null)
        {
            var separator = item.LastIndexOfAny(new[] { '/', '\\' });
            var file = new Mock<IPendingChange>();
            file.SetupGet(c => c.ItemId).Returns(itemId);
            file.SetupGet(c => c.LocalOrServerItem).Returns(item);
            file.SetupGet(c => c.LocalOrServerFolder).Returns(item.Substring(0, separator));
            file.SetupGet(c => c.FileName).Returns(item.Substring(separator + 1));
            file.SetupGet(c => c.ServerItem).Returns(serverItem ?? item);
            file.SetupGet(c => c.ChangeType).Returns(changeType);
            file.SetupGet(c => c.UploadHashValue).Returns(hash == 0 ? null : new[] { hash });
            file.Setup(c => c.DownloadShelvedFile()).Returns(() => new System.IO.MemoryStream());
            return file.Object;
        }
    }
}
