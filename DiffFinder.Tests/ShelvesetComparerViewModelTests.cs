using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the order and filter <see cref="ShelvesetComparerViewModel"/> lists the files in.
    /// </summary>
    [TestClass]
    public class ShelvesetComparerViewModelTests
    {
        private readonly FileComparisonViewModel readme = Row("$/P", "README.md");
        private readonly FileComparisonViewModel nested = Row("$/P/src", "a.cs");
        private readonly FileComparisonViewModel source = Row("$/P", "b.cs");

        [TestMethod]
        public void FileOrder_DefaultsToTheFolderTree()
        {
            Assert.AreEqual(FileOrder.Tree, new ShelvesetComparerViewModel(null).FileOrder);
        }

        [TestMethod]
        public void Files_AreListedInTheChosenOrder()
        {
            var model = new ShelvesetComparerViewModel(null);
            model.SetFiles(new[] { this.readme, this.nested, this.source });

            CollectionAssert.AreEqual(new[] { this.nested, this.source, this.readme }, model.Files.ToArray());

            model.FileOrder = FileOrder.Path;
            CollectionAssert.AreEqual(new[] { this.source, this.readme, this.nested }, model.Files.ToArray());

            model.FileOrder = FileOrder.Shelveset;
            CollectionAssert.AreEqual(new[] { this.readme, this.nested, this.source }, model.Files.ToArray());
        }

        [TestMethod]
        public void FileOrder_Changed_RefreshesTheFiles()
        {
            var model = new ShelvesetComparerViewModel(null);
            var changed = new List<string>();
            model.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            model.FileOrder = FileOrder.Path;

            CollectionAssert.Contains(changed, nameof(ShelvesetComparerViewModel.FileOrder));
            CollectionAssert.Contains(changed, nameof(ShelvesetComparerViewModel.Files));
        }

        [TestMethod]
        public void Files_Filtered_KeepTheChosenOrder()
        {
            var model = new ShelvesetComparerViewModel(null) { FileOrder = FileOrder.Path };
            model.SetFiles(new[] { this.nested, this.readme, this.source });

            model.Filter = ".cs";

            CollectionAssert.AreEqual(new[] { this.source, this.nested }, model.Files.ToArray());
        }

        private static FileComparisonViewModel Row(string folder, string fileName)
        {
            var file = new Mock<IPendingChange>();
            file.SetupGet(f => f.LocalOrServerFolder).Returns(folder);
            file.SetupGet(f => f.FileName).Returns(fileName);
            file.SetupGet(f => f.ServerItem).Returns(folder + "/" + fileName);
            return new FileComparisonViewModel { FirstFile = file.Object };
        }
    }
}
