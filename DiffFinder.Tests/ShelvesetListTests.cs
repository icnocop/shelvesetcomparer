using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the shelveset list of Views\SelectShelvesetTeamExplorerView.xaml, rendered from the view
    /// itself, listing the shelvesets of a <see cref="ShelvesetPager"/>.
    /// </summary>
    [TestClass]
    public class ShelvesetListTests
    {
        private Window window;
        private FrameworkElement panel;
        private ShelvesetPager pager;
        private ShelvesetViewModel[] shelvesets;

        [TestCleanup]
        public void Cleanup()
        {
            this.window?.Close();
        }

        [STATestMethod]
        public void List_ShowsOnlyTheCurrentPage()
        {
            this.Show(30);

            Assert.AreEqual(ShelvesetPager.DefaultPageSize, this.List.Items.Count);
            Assert.AreSame(this.shelvesets[0], this.List.Items[0]);
        }

        [STATestMethod]
        public void NextPageButton_ShowsTheNextPage()
        {
            this.Show(30);

            Invoke(this.Find<Button>("NextPageButton"));

            Assert.AreEqual(5, this.List.Items.Count);
            Assert.AreSame(this.shelvesets[25], this.List.Items[0]);
            Assert.AreEqual("Page 2 of 2 (30 shelvesets)", this.Find<TextBlock>("PageText").Text);
            Assert.IsFalse(this.Find<Button>("NextPageButton").IsEnabled);
            Assert.IsTrue(this.Find<Button>("PreviousPageButton").IsEnabled);
        }

        [STATestMethod]
        public void PreviousPageButton_OnTheFirstPage_IsDisabled()
        {
            this.Show(30);

            Assert.IsFalse(this.Find<Button>("PreviousPageButton").IsEnabled);
            Assert.AreEqual("Page 1 of 2 (30 shelvesets)", this.Find<TextBlock>("PageText").Text);
        }

        [STATestMethod]
        public void ColumnHeader_Click_SortsAllTheShelvesets()
        {
            this.Show(30);
            var header = ViewXaml.FindDescendants<GridViewColumnHeader>(this.List).Single(h => "Shelveset Name".Equals(h.Content));

            header.RaiseEvent(new RoutedEventArgs(GridViewColumnHeader.ClickEvent, header));
            ViewXaml.DoEvents();

            Assert.AreEqual(nameof(ShelvesetViewModel.Name), this.pager.SortPropertyName);
            Assert.IsTrue(AdornerLayer.GetAdornerLayer(header).GetAdorners(header)?.Any(a => a.GetType().Name == "SortGlyphAdorner") == true, "no sort glyph shown");
        }

        [STATestMethod]
        public void ReviewersColumn_ListsTheReviewers()
        {
            this.Show(3, configure: s => s[1].Reviewers = new[] { "Ann Lee", "Bob Jones" });

            Assert.IsTrue(ViewXaml.FindDescendants<TextBlock>(this.List).Any(t => t.Text == "Ann Lee, Bob Jones"));
            Assert.IsTrue(ViewXaml.FindDescendants<GridViewColumnHeader>(this.List).Any(h => "Reviewers".Equals(h.Content)));
        }

        [STATestMethod]
        public void ShelvesetAssignedToMe_IsBold()
        {
            this.Show(3, configure: s => s[1].IsAssignedToMe = true);

            var rows = ViewXaml.FindDescendants<ListViewItem>(this.List).ToList();

            Assert.AreEqual(FontWeights.Bold, rows.Single(r => r.DataContext == this.shelvesets[1]).FontWeight);
            Assert.AreEqual(FontWeights.Normal, rows.Single(r => r.DataContext == this.shelvesets[0]).FontWeight);
        }

        [STATestMethod]
        public void SelectionOnAnotherPage_IsNamed()
        {
            this.Show(30);

            this.pager.Select(this.shelvesets[0]);
            this.pager.Select(this.shelvesets[27]);
            ViewXaml.DoEvents();

            Assert.AreEqual("Selected: s1, s28", this.Find<TextBlock>("SelectionText").Text);
        }

        private ListView List
        {
            get
            {
                return this.Find<ListView>("ListShelvesets");
            }
        }

        private void Show(int count, Action<ShelvesetViewModel[]> configure = null)
        {
            this.shelvesets = Enumerable.Range(1, count).Select(i => new ShelvesetViewModel("s" + i, new DateTime(2026, 1, 1).AddDays(i), "owner")).ToArray();
            configure?.Invoke(this.shelvesets);
            this.pager = new ShelvesetPager();
            this.pager.SetItems(this.shelvesets);

            this.panel = ViewXaml.Load<FrameworkElement>("SelectShelvesetTeamExplorerView.xaml", "ShelvesetComparisonPanel");

            // the view is its own data context, which is where the bindings find the section
            this.panel.DataContext = new { ParentSection = new { Pager = this.pager } };
            this.window = ViewXaml.Show(this.panel);
        }

        private T Find<T>(string name)
            where T : FrameworkElement
        {
            return ViewXaml.FindDescendants<T>(this.panel).Single(e => e.Name == name);
        }

        private static void Invoke(Button button)
        {
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            ViewXaml.DoEvents();
        }
    }
}
