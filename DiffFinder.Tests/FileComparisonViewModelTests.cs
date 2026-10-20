using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for pairing two files the comparison grid did not align with
    /// <see cref="FileComparisonViewModel.CreateUnaligned"/>.
    /// </summary>
    [TestClass]
    public class FileComparisonViewModelTests
    {
        private readonly IPendingChange oldName = new Mock<IPendingChange>().Object;
        private readonly IPendingChange newName = new Mock<IPendingChange>().Object;

        [TestMethod]
        public void CreateUnaligned_FirstToSecond_KeepsTheFirstShelvesetOnTheLeft()
        {
            var source = new FileComparisonViewModel { FirstFile = this.oldName, FirstShelveName = "review 1" };
            var target = new FileComparisonViewModel { SecondFile = this.newName, SecondShelveName = "review 2" };

            var pair = FileComparisonViewModel.CreateUnaligned(source, ComparisonSide.First, target, ComparisonSide.Second);

            Assert.AreSame(this.oldName, pair.FirstFile);
            Assert.AreEqual("review 1", pair.FirstShelveName);
            Assert.AreSame(this.newName, pair.SecondFile);
            Assert.AreEqual("review 2", pair.SecondShelveName);
        }

        [TestMethod]
        public void CreateUnaligned_SecondToFirst_KeepsTheFirstShelvesetOnTheLeft()
        {
            var source = new FileComparisonViewModel { SecondFile = this.newName, SecondShelveName = "review 2" };
            var target = new FileComparisonViewModel { FirstFile = this.oldName, FirstShelveName = "review 1" };

            var pair = FileComparisonViewModel.CreateUnaligned(source, ComparisonSide.Second, target, ComparisonSide.First);

            Assert.AreSame(this.oldName, pair.FirstFile);
            Assert.AreEqual("review 1", pair.FirstShelveName);
            Assert.AreSame(this.newName, pair.SecondFile);
            Assert.AreEqual("review 2", pair.SecondShelveName);
        }

        [TestMethod]
        public void CreateUnaligned_SameShelveset_PutsTheFilePickedFirstOnTheLeft()
        {
            var source = new FileComparisonViewModel { SecondFile = this.newName, SecondShelveName = "review 2" };
            var target = new FileComparisonViewModel { SecondFile = this.oldName, SecondShelveName = "review 2" };

            var pair = FileComparisonViewModel.CreateUnaligned(source, ComparisonSide.Second, target, ComparisonSide.Second);

            Assert.AreSame(this.newName, pair.FirstFile);
            Assert.AreSame(this.oldName, pair.SecondFile);
            Assert.AreEqual("review 2", pair.FirstShelveName);
            Assert.AreEqual("review 2", pair.SecondShelveName);
        }

        [TestMethod]
        public void FileToolTip_ServerFile_ShowsTheFullPathAndTheChange()
        {
            var row = new FileComparisonViewModel
            {
                FirstFile = File("$/Project/Main/src", "Program.cs", "$/Project/Main/src/Program.cs", ChangeType.Edit),
                SecondFile = File("$/Project/Dev/src/very/deep/folder", "Program.cs", "$/Project/Dev/src/very/deep/folder/Program.cs", ChangeType.Delete)
            };

            Assert.AreEqual("$/Project/Main/src/Program.cs\r\nChange: edit", row.FirstFileToolTip);
            Assert.AreEqual("$/Project/Dev/src/very/deep/folder/Program.cs\r\nChange: delete", row.SecondFileToolTip);
        }

        [TestMethod]
        public void FileToolTip_LocalFile_AlsoShowsTheServerPath()
        {
            var row = new FileComparisonViewModel
            {
                FirstFile = File(@"C:\Source\Main\src", "Program.cs", "$/Project/Main/src/Program.cs", ChangeType.Edit | ChangeType.Encoding)
            };

            Assert.AreEqual(@"C:\Source\Main\src/Program.cs" + "\r\n$/Project/Main/src/Program.cs\r\nChange: edit, encoding", row.FirstFileToolTip);
        }

        [TestMethod]
        public void FileToolTip_NoFileOnThatSide_IsNull()
        {
            var row = new FileComparisonViewModel
            {
                FirstFile = File("$/Project/Main/src", "Program.cs", "$/Project/Main/src/Program.cs", ChangeType.Add)
            };

            // null rather than empty, so that no empty tooltip pops up over the missing file
            Assert.IsNull(row.SecondFileToolTip);
        }

        private static IPendingChange File(string folder, string fileName, string serverItem, ChangeType changeType)
        {
            var file = new Mock<IPendingChange>();
            file.SetupGet(f => f.LocalOrServerFolder).Returns(folder);
            file.SetupGet(f => f.FileName).Returns(fileName);
            file.SetupGet(f => f.ServerItem).Returns(serverItem);
            file.SetupGet(f => f.ChangeType).Returns(changeType);
            return file.Object;
        }
    }
}
