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
    }
}
