using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the comparison grid of the comparison summary, and for <see cref="ComparisonGridHitTest"/>,
    /// which tells which file of the grid a mouse event happened on. The grid is rendered from the view
    /// itself, so that the elements the mouse events come from are the real ones.
    /// </summary>
    [TestClass]
    public class ComparisonGridTests
    {
        private Window window;
        private ListView grid;
        private FileComparisonViewModel row;

        [TestCleanup]
        public void Cleanup()
        {
            this.window?.Close();
        }

        [STATestMethod]
        public void TryGetCell_LeftFile_ResolvesTheFirstSide()
        {
            this.ShowGrid();

            var hit = this.HitTestMiddleOf(this.FindCellText("FirstFileNameTxt"));

            Assert.IsTrue(ComparisonGridHitTest.TryGetCell(hit, out var cellRow, out var side), $"no cell found for {hit}");
            Assert.AreSame(this.row, cellRow);
            Assert.AreEqual(ComparisonSide.First, side);
        }

        [STATestMethod]
        public void TryGetCell_RightFile_ResolvesTheSecondSide()
        {
            this.ShowGrid();

            var hit = this.HitTestMiddleOf(this.FindCellText("SecondFileNameTxt"));

            Assert.IsTrue(ComparisonGridHitTest.TryGetCell(hit, out var cellRow, out var side), $"no cell found for {hit}");
            Assert.AreSame(this.row, cellRow);
            Assert.AreEqual(ComparisonSide.Second, side);
        }

        [STATestMethod]
        public void TryGetCell_EmptyPartOfTheLeftCell_ResolvesTheFirstSide()
        {
            // a column widened past its text, which is what resizing a column does
            this.ShowGrid(firstColumnWidth: 600);

            var text = this.FindCellText("FirstFileNameTxt");
            var cellEnd = text.TranslatePoint(new Point(0, text.ActualHeight / 2), this.window);
            var hit = this.HitTest(new Point(cellEnd.X + 550, cellEnd.Y));

            Assert.IsTrue(ComparisonGridHitTest.TryGetCell(hit, out var cellRow, out var side), $"no cell found for {hit}");
            Assert.AreSame(this.row, cellRow);
            Assert.AreEqual(ComparisonSide.First, side);
        }

        [STATestMethod]
        public void TryGetCell_EmptyPartOfTheRightCell_ResolvesTheSecondSide()
        {
            this.ShowGrid(secondColumnWidth: 600);

            var text = this.FindCellText("SecondFileNameTxt");
            var cellStart = text.TranslatePoint(new Point(0, text.ActualHeight / 2), this.window);
            var hit = this.HitTest(new Point(cellStart.X + 550, cellStart.Y));

            Assert.IsTrue(ComparisonGridHitTest.TryGetCell(hit, out var cellRow, out var side), $"no cell found for {hit}");
            Assert.AreSame(this.row, cellRow);
            Assert.AreEqual(ComparisonSide.Second, side);
        }

        [STATestMethod]
        public void TryGetCell_ColumnHeader_FindsNoCell()
        {
            this.ShowGrid();

            Assert.IsFalse(ComparisonGridHitTest.TryGetCell(this.FindHeaderGripper(), out _, out _));
        }

        [STATestMethod]
        public void IsInRow_File_IsTrue()
        {
            this.ShowGrid();

            Assert.IsTrue(ComparisonGridHitTest.IsInRow(this.FindCellText("FirstFileNameTxt")));
            Assert.IsTrue(ComparisonGridHitTest.IsInRow(this.FindCellText("SecondFileNameTxt")));
        }

        [STATestMethod]
        public void IsInRow_ColumnSplitter_IsFalse()
        {
            this.ShowGrid();

            Assert.IsFalse(ComparisonGridHitTest.IsInRow(this.FindHeaderGripper()));
        }

        [STATestMethod]
        public void IsInRow_ColumnHeader_IsFalse()
        {
            this.ShowGrid();

            var header = ViewXaml.FindDescendants<GridViewColumnHeader>(this.grid).First(h => h.Column != null);

            Assert.IsFalse(ComparisonGridHitTest.IsInRow(header));
        }

        [STATestMethod]
        public void File_HasItsFullPathAsTooltip()
        {
            this.ShowGrid();

            Assert.AreEqual(this.row.FirstFileToolTip, this.FindCellText("FirstFileNameTxt").ToolTip);
            Assert.AreEqual(this.row.SecondFileToolTip, this.FindCellText("SecondFileNameTxt").ToolTip);
            StringAssert.StartsWith((string)this.FindCellText("FirstFileNameTxt").ToolTip, "$/Project/Main/src/Program.cs");
        }

        [TestMethod]
        public void IsInRow_Null_IsFalse()
        {
            Assert.IsFalse(ComparisonGridHitTest.IsInRow(null));
        }

        private void ShowGrid(double? firstColumnWidth = null, double? secondColumnWidth = null)
        {
            this.row = new FileComparisonViewModel
            {
                FirstFile = File("$/Project/Main/src", "Program.cs"),
                SecondFile = File("$/Project/Dev/src", "Program.cs"),
                Status = FileComparisonStatus.Different
            };

            this.grid = ViewXaml.Load<ListView>("MainView.xaml", "ComparisonFiles");
            this.grid.ItemsSource = new[] { this.row };
            if (firstColumnWidth.HasValue)
            {
                ((GridView)this.grid.View).Columns[0].Width = firstColumnWidth.Value;
            }

            if (secondColumnWidth.HasValue)
            {
                ((GridView)this.grid.View).Columns[1].Width = secondColumnWidth.Value;
            }

            this.window = ViewXaml.Show(this.grid);
        }

        private TextBlock FindCellText(string name)
        {
            return ViewXaml.FindDescendants<TextBlock>(this.grid).Single(t => t.Name == name);
        }

        private Thumb FindHeaderGripper()
        {
            return ViewXaml.FindDescendants<Thumb>(this.grid).First(t => t.Name == "PART_HeaderGripper" && t.IsVisible);
        }

        private DependencyObject HitTestMiddleOf(FrameworkElement element)
        {
            return this.HitTest(element.TranslatePoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2), this.window));
        }

        private DependencyObject HitTest(Point point)
        {
            // what the mouse events report as their original source
            return this.window.InputHitTest(point) as DependencyObject;
        }

        private static IPendingChange File(string folder, string fileName)
        {
            var file = new Mock<IPendingChange>();
            file.SetupGet(f => f.LocalOrServerFolder).Returns(folder);
            file.SetupGet(f => f.FileName).Returns(fileName);
            file.SetupGet(f => f.LocalOrServerItem).Returns(folder + "/" + fileName);
            file.SetupGet(f => f.ServerItem).Returns(folder + "/" + fileName);
            return file.Object;
        }
    }
}
