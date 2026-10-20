using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="ShelvesetPager"/>, which lists the shelvesets a page at a time while keeping the
    /// sort order and the selection across all of them.
    /// </summary>
    [TestClass]
    public class ShelvesetPagerTests
    {
        [TestMethod]
        public void PageSize_DefaultsTo25()
        {
            Assert.AreEqual(25, new ShelvesetPager().PageSize);
        }

        [TestMethod]
        public void CurrentPage_ListsThePageSizeFirstShelvesets()
        {
            var pager = new ShelvesetPager(pageSize: 3);

            pager.SetItems(Shelvesets(7));

            CollectionAssert.AreEqual(new[] { "s1", "s2", "s3" }, Names(pager.CurrentPage));
            Assert.AreEqual(0, pager.PageIndex);
            Assert.AreEqual(3, pager.PageCount);
            Assert.IsFalse(pager.CanGoPrevious);
            Assert.IsTrue(pager.CanGoNext);
        }

        [TestMethod]
        public void Next_GoesThroughThePagesUpToTheLast()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(7));

            pager.Next();
            CollectionAssert.AreEqual(new[] { "s4", "s5", "s6" }, Names(pager.CurrentPage));

            pager.Next();
            CollectionAssert.AreEqual(new[] { "s7" }, Names(pager.CurrentPage));
            Assert.IsFalse(pager.CanGoNext);
            Assert.IsTrue(pager.CanGoPrevious);

            pager.Next();
            CollectionAssert.AreEqual(new[] { "s7" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void Previous_GoesBackUpToTheFirstPage()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(7));
            pager.Next();

            pager.Previous();
            pager.Previous();

            Assert.AreEqual(0, pager.PageIndex);
            CollectionAssert.AreEqual(new[] { "s1", "s2", "s3" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void NoShelvesets_IsOneEmptyPage()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(null);

            Assert.AreEqual(1, pager.PageCount);
            Assert.AreEqual(0, pager.CurrentPage.Count);
            Assert.IsFalse(pager.CanGoNext);
            Assert.IsFalse(pager.CanGoPrevious);
        }

        [TestMethod]
        public void SetItems_FewerShelvesets_StaysOnTheLastPageThereIs()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(9));
            pager.Next();
            pager.Next();

            pager.SetItems(Shelvesets(5));

            Assert.AreEqual(1, pager.PageIndex);
            CollectionAssert.AreEqual(new[] { "s4", "s5" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void PageText_TellsThePageAndTheNumberOfShelvesets()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(7));
            pager.Next();

            Assert.AreEqual("Page 2 of 3 (7 shelvesets)", pager.PageText);
        }

        [TestMethod]
        public void Next_RaisesPropertyChangedForThePage()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(7));
            var changed = new List<string>();
            pager.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            pager.Next();

            CollectionAssert.IsSubsetOf(
                new[] { nameof(ShelvesetPager.CurrentPage), nameof(ShelvesetPager.PageIndex), nameof(ShelvesetPager.PageText), nameof(ShelvesetPager.CanGoNext), nameof(ShelvesetPager.CanGoPrevious) },
                changed);
        }

        [TestMethod]
        public void Sort_SortsAllTheShelvesetsAndGoesBackToTheFirstPage()
        {
            var pager = new ShelvesetPager(pageSize: 2);
            pager.SetItems(new[] { Shelveset("b"), Shelveset("d"), Shelveset("a"), Shelveset("c") });
            pager.Next();

            pager.Sort(nameof(ShelvesetViewModel.Name));

            Assert.AreEqual(0, pager.PageIndex);
            CollectionAssert.AreEqual(new[] { "a", "b" }, Names(pager.CurrentPage));
            pager.Next();
            CollectionAssert.AreEqual(new[] { "c", "d" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void Sort_SameColumnAgain_SortsDescending()
        {
            var pager = new ShelvesetPager(pageSize: 2);
            pager.SetItems(new[] { Shelveset("b"), Shelveset("d"), Shelveset("a"), Shelveset("c") });

            pager.Sort(nameof(ShelvesetViewModel.Name));
            pager.Sort(nameof(ShelvesetViewModel.Name));

            CollectionAssert.AreEqual(new[] { "d", "c" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void Sort_ByDate_ComparesTheDatesNotTheirText()
        {
            var pager = new ShelvesetPager(pageSize: 5);
            var older = new ShelvesetViewModel("older", new DateTime(2026, 2, 1), "owner");
            var newer = new ShelvesetViewModel("newer", new DateTime(2026, 10, 1), "owner");
            pager.SetItems(new[] { newer, older });

            pager.Sort(nameof(ShelvesetViewModel.CreationDate));

            CollectionAssert.AreEqual(new[] { "older", "newer" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void SetItems_KeepsTheSortOrder()
        {
            var pager = new ShelvesetPager(pageSize: 5);
            pager.SetItems(new[] { Shelveset("b"), Shelveset("a") });
            pager.Sort(nameof(ShelvesetViewModel.Name));

            pager.SetItems(new[] { Shelveset("z"), Shelveset("c") });

            CollectionAssert.AreEqual(new[] { "c", "z" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void PageCommands_FollowThePages()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(5));
            var raised = 0;
            pager.NextPageCommand.CanExecuteChanged += (sender, e) => raised++;

            Assert.IsTrue(pager.NextPageCommand.CanExecute(null));
            Assert.IsFalse(pager.PreviousPageCommand.CanExecute(null));

            pager.NextPageCommand.Execute(null);

            Assert.AreEqual(1, pager.PageIndex);
            Assert.IsFalse(pager.NextPageCommand.CanExecute(null));
            Assert.IsTrue(pager.PreviousPageCommand.CanExecute(null));
            Assert.IsTrue(raised > 0);

            pager.PreviousPageCommand.Execute(null);

            Assert.AreEqual(0, pager.PageIndex);
        }

        [TestMethod]
        public void SortCommand_SortsByTheGivenProperty()
        {
            var pager = new ShelvesetPager(pageSize: 5);
            pager.SetItems(new[] { Shelveset("b"), Shelveset("a") });

            Assert.IsTrue(pager.SortCommand.CanExecute(nameof(ShelvesetViewModel.Name)));
            pager.SortCommand.Execute(nameof(ShelvesetViewModel.Name));

            CollectionAssert.AreEqual(new[] { "a", "b" }, Names(pager.CurrentPage));
        }

        [TestMethod]
        public void GoToPage_ShowsThatPageWithinTheRangeOfPages()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            pager.SetItems(Shelvesets(7));

            pager.GoToPage(1);
            CollectionAssert.AreEqual(new[] { "s4", "s5", "s6" }, Names(pager.CurrentPage));

            pager.GoToPage(10);
            Assert.AreEqual(2, pager.PageIndex);

            pager.GoToPage(-1);
            Assert.AreEqual(0, pager.PageIndex);
        }

        [TestMethod]
        public void SetSort_SortsInTheGivenDirection()
        {
            var pager = new ShelvesetPager(pageSize: 5);
            pager.SetItems(new[] { Shelveset("b"), Shelveset("a"), Shelveset("c") });

            pager.SetSort(nameof(ShelvesetViewModel.Name), System.ComponentModel.ListSortDirection.Descending);

            CollectionAssert.AreEqual(new[] { "c", "b", "a" }, Names(pager.CurrentPage));
            Assert.AreEqual(nameof(ShelvesetViewModel.Name), pager.SortPropertyName);
            Assert.AreEqual(System.ComponentModel.ListSortDirection.Descending, pager.SortDirection);
        }

        [TestMethod]
        public void Select_ShelvesetsOnDifferentPages_AreBothSelected()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7);
            pager.SetItems(shelvesets);

            pager.Select(shelvesets[0]);
            pager.Next();
            pager.Next();
            pager.Select(shelvesets[6]);

            CollectionAssert.AreEqual(new[] { shelvesets[0], shelvesets[6] }, pager.SelectedShelvesets.ToArray());
            Assert.IsTrue(shelvesets[0].IsSelected);
            Assert.IsTrue(shelvesets[6].IsSelected);
        }

        [TestMethod]
        public void Select_AThirdShelveset_DeselectsTheOneSelectedFirst()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7);
            pager.SetItems(shelvesets);

            pager.Select(shelvesets[0]);
            pager.Select(shelvesets[4]);
            pager.Select(shelvesets[6]);

            CollectionAssert.AreEqual(new[] { shelvesets[4], shelvesets[6] }, pager.SelectedShelvesets.ToArray());
            Assert.IsFalse(shelvesets[0].IsSelected);
        }

        [TestMethod]
        public void Deselect_RemovesTheShelvesetFromTheSelection()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7);
            pager.SetItems(shelvesets);
            pager.Select(shelvesets[0]);
            pager.Select(shelvesets[4]);

            pager.Deselect(shelvesets[0]);

            CollectionAssert.AreEqual(new[] { shelvesets[4] }, pager.SelectedShelvesets.ToArray());
            Assert.IsFalse(shelvesets[0].IsSelected);
        }

        [TestMethod]
        public void SetItems_ShelvesetsMarkedSelected_AreTheSelection()
        {
            // restoring the list the section was left with restores the selection the items carry
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7);
            shelvesets[1].IsSelected = true;
            shelvesets[5].IsSelected = true;

            pager.SetItems(shelvesets);

            CollectionAssert.AreEqual(new[] { shelvesets[1], shelvesets[5] }, pager.SelectedShelvesets.ToArray());
        }

        [TestMethod]
        public void SelectionText_NamesTheSelectedShelvesets()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7);
            pager.SetItems(shelvesets);

            Assert.AreEqual(string.Empty, pager.SelectionText);

            pager.Select(shelvesets[0]);
            pager.Select(shelvesets[6]);

            Assert.AreEqual("Selected: s1, s7", pager.SelectionText);
        }

        [TestMethod]
        public void PageShown_ReportsTheShelvesetsOfThePageWhoseDetailsAreNotLoaded()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var shelvesets = Shelvesets(7, withDetails: false);
            var shown = new List<ShelvesetViewModel[]>();
            pager.PageShown += (sender, e) => shown.Add(e.ShelvesetsWithoutDetails.ToArray());

            pager.SetItems(shelvesets);
            shelvesets[3].LoadDetails();
            pager.Next();

            Assert.AreEqual(2, shown.Count);
            CollectionAssert.AreEqual(new[] { shelvesets[0], shelvesets[1], shelvesets[2] }, shown[0]);
            CollectionAssert.AreEqual(new[] { shelvesets[4], shelvesets[5] }, shown[1]);
        }

        [TestMethod]
        public void PageShown_EveryDetailLoaded_IsNotRaised()
        {
            var pager = new ShelvesetPager(pageSize: 3);
            var raised = false;
            pager.PageShown += (sender, e) => raised = true;

            pager.SetItems(Shelvesets(2));

            Assert.IsFalse(raised);
        }

        private static ShelvesetViewModel Shelveset(string name)
        {
            return new ShelvesetViewModel(name, DateTime.Now, "owner");
        }

        private static ShelvesetViewModel[] Shelvesets(int count, bool withDetails = true)
        {
            return Enumerable.Range(1, count)
                .Select(i => withDetails
                    ? Shelveset("s" + i)
                    : new ShelvesetViewModel("s" + i, DateTime.Now, "owner", workItemsLoader: () => new ShelvesetWorkItemViewModel[0]))
                .ToArray();
        }

        private static string[] Names(IEnumerable<ShelvesetViewModel> shelvesets)
        {
            return shelvesets.Select(s => s.Name).ToArray();
        }
    }
}
