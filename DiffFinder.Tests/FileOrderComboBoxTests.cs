using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the drop down list of Views\MainView.xaml switching the order the comparison summary lists
    /// its files in.
    /// </summary>
    [TestClass]
    public class FileOrderComboBoxTests
    {
        private Window window;
        private ComboBox comboBox;
        private ShelvesetComparerViewModel model;

        [TestCleanup]
        public void Cleanup()
        {
            this.window?.Close();
        }

        [STATestMethod]
        public void ShowsTheOrderOfTheComparison()
        {
            this.Show();

            Assert.AreEqual(FileOrder.Tree, this.comboBox.SelectedValue);

            this.model.FileOrder = FileOrder.Shelveset;
            ViewXaml.DoEvents();

            Assert.AreEqual(FileOrder.Shelveset, this.comboBox.SelectedValue);
        }

        [STATestMethod]
        public void OffersEveryOrder()
        {
            this.Show();

            CollectionAssert.AreEquivalent(
                new[] { FileOrder.Tree, FileOrder.Path, FileOrder.Shelveset },
                this.comboBox.Items.OfType<FrameworkElement>().Select(item => item.Tag).ToArray());
        }

        [STATestMethod]
        public void PickingAnOrder_ChangesTheOrderOfTheComparison()
        {
            this.Show();

            this.comboBox.SelectedItem = this.comboBox.Items.OfType<FrameworkElement>().Single(item => FileOrder.Path.Equals(item.Tag));
            ViewXaml.DoEvents();

            Assert.AreEqual(FileOrder.Path, this.model.FileOrder);
        }

        private void Show()
        {
            this.model = new ShelvesetComparerViewModel(null);
            this.comboBox = ViewXaml.Load<ComboBox>("MainView.xaml", "FileOrderComboBox");

            // the view is its own data context, which is where the binding finds the comparison
            this.comboBox.DataContext = new { ComparisonModel = this.model };
            this.window = ViewXaml.Show(this.comboBox);
        }
    }
}
