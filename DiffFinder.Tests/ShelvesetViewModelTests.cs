using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the comment and work items <see cref="ShelvesetViewModel"/> lists a shelveset with.
    /// </summary>
    [TestClass]
    public class ShelvesetViewModelTests
    {
        [DataTestMethod]
        [DataRow(null, "")]
        [DataRow("", "")]
        [DataRow("  \r\n \n", "")]
        [DataRow("Fix the login", "Fix the login")]
        [DataRow("\r\n  Fix the login  \r\nDetails follow", "Fix the login")]
        [DataRow("Fix the login\nDetails follow", "Fix the login")]
        public void GetFirstLine_ReturnsTheFirstNonBlankLineTrimmed(string comment, string expected)
        {
            Assert.AreEqual(expected, ShelvesetViewModel.GetFirstLine(comment));
        }

        [TestMethod]
        public void CommentFirstLine_IsTheFirstLineOfTheComment()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", comment: "Fix the login\r\nDetails");

            Assert.AreEqual("Fix the login\r\nDetails", shelveset.Comment);
            Assert.AreEqual("Fix the login", shelveset.CommentFirstLine);
        }

        [TestMethod]
        public void HasWorkItems_NoWorkItems_IsFalse()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith");

            Assert.IsFalse(shelveset.HasWorkItems);
            Assert.AreEqual(0, shelveset.WorkItems.Count);
            Assert.AreEqual(string.Empty, shelveset.Comment);
        }

        [TestMethod]
        public void HasWorkItems_WithWorkItems_IsTrue()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", workItems: new[] { new ShelvesetWorkItemViewModel(12) });

            Assert.IsTrue(shelveset.HasWorkItems);
        }

        [TestMethod]
        public void IsExpanded_Changed_RaisesPropertyChanged()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith");
            string changed = null;
            shelveset.PropertyChanged += (sender, e) => changed = e.PropertyName;

            shelveset.IsExpanded = true;

            Assert.AreEqual(nameof(ShelvesetViewModel.IsExpanded), changed);
        }

        [TestMethod]
        public void MergeWorkItems_KeepsTheDescribedWorkItemsAndAddsTheIdsOnlyListed()
        {
            var described = new[] { new ShelvesetWorkItemViewModel(12, "Fix the login", "Task", "Active") };

            var workItems = ShelvesetViewModel.MergeWorkItems(described, new[] { 12, 34 });

            CollectionAssert.AreEqual(new[] { 12, 34 }, workItems.Select(workItem => workItem.Id).ToArray());
            Assert.AreEqual("Fix the login", workItems[0].Title);
            Assert.AreEqual(string.Empty, workItems[1].Title);
        }

        [TestMethod]
        public void MergeWorkItems_Nothing_ReturnsNoWorkItems()
        {
            Assert.AreEqual(0, ShelvesetViewModel.MergeWorkItems(null, null).Count);
        }

        [TestMethod]
        public void WorkItems_NotReadUntilTheDetailsAreLoaded()
        {
            var read = 0;
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", workItemsLoader: () =>
            {
                read++;
                return new[] { new ShelvesetWorkItemViewModel(12) };
            });

            Assert.IsFalse(shelveset.AreDetailsLoaded);
            Assert.AreEqual(0, shelveset.WorkItems.Count);
            Assert.IsFalse(shelveset.HasWorkItems);
            Assert.AreEqual(0, read);

            shelveset.LoadDetails();
            shelveset.LoadDetails();

            Assert.IsTrue(shelveset.AreDetailsLoaded);
            Assert.AreEqual(1, read);
            Assert.AreEqual(12, shelveset.WorkItems.Single().Id);
            Assert.IsTrue(shelveset.HasWorkItems);
        }

        [TestMethod]
        public void LoadDetails_NotifiesTheWorkItems()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", workItemsLoader: () => new[] { new ShelvesetWorkItemViewModel(12) });
            var changed = new System.Collections.Generic.List<string>();
            shelveset.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            shelveset.LoadDetails();

            CollectionAssert.Contains(changed, nameof(ShelvesetViewModel.WorkItems));
            CollectionAssert.Contains(changed, nameof(ShelvesetViewModel.HasWorkItems));
        }

        [TestMethod]
        public void LoadDetails_LoaderFails_LeavesNoWorkItems()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", workItemsLoader: () => throw new InvalidOperationException("server unreachable"));

            shelveset.LoadDetails();

            Assert.IsTrue(shelveset.AreDetailsLoaded);
            Assert.AreEqual(0, shelveset.WorkItems.Count);
        }

        [TestMethod]
        public void WorkItemsGiven_AreLoadedFromTheStart()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith", workItems: new[] { new ShelvesetWorkItemViewModel(12) });

            Assert.IsTrue(shelveset.AreDetailsLoaded);
            Assert.IsTrue(shelveset.HasWorkItems);
        }

        [TestMethod]
        public void Reviewers_NoneByDefault()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith");

            Assert.AreEqual(0, shelveset.Reviewers.Count);
            Assert.AreEqual(string.Empty, shelveset.ReviewersText);
            Assert.IsFalse(shelveset.IsAssignedToMe);
        }

        [TestMethod]
        public void Reviewers_Set_AreListedAndNotified()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith");
            var changed = new System.Collections.Generic.List<string>();
            shelveset.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            shelveset.Reviewers = new[] { "Ann Lee", "Bob Jones" };

            Assert.AreEqual("Ann Lee, Bob Jones", shelveset.ReviewersText);
            CollectionAssert.Contains(changed, nameof(ShelvesetViewModel.Reviewers));
            CollectionAssert.Contains(changed, nameof(ShelvesetViewModel.ReviewersText));
        }

        [TestMethod]
        public void IsReviewer_IgnoresCasing()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith") { Reviewers = new[] { "Ann Lee" } };

            Assert.IsTrue(shelveset.IsReviewer("ann lee"));
            Assert.IsFalse(shelveset.IsReviewer("Bob Jones"));
            Assert.IsFalse(shelveset.IsReviewer(null));
        }

        [TestMethod]
        public void IsAssignedToMe_Changed_RaisesPropertyChanged()
        {
            var shelveset = new ShelvesetViewModel("review", DateTime.Now, "John Smith");
            string changed = null;
            shelveset.PropertyChanged += (sender, e) => changed = e.PropertyName;

            shelveset.IsAssignedToMe = true;

            Assert.AreEqual(nameof(ShelvesetViewModel.IsAssignedToMe), changed);
        }

        [TestMethod]
        public void MergeWorkItems_DuplicateIds_ListsEachWorkItemOnce()
        {
            var described = new[] { new ShelvesetWorkItemViewModel(12, "Fix the login"), new ShelvesetWorkItemViewModel(12, "Fix the login"), null };

            var workItems = ShelvesetViewModel.MergeWorkItems(described, new[] { 12, 12 });

            Assert.AreEqual(1, workItems.Count);
        }
    }
}
