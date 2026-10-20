using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the line <see cref="ShelvesetWorkItemViewModel"/> lists a work item with.
    /// </summary>
    [TestClass]
    public class ShelvesetWorkItemViewModelTests
    {
        [TestMethod]
        public void DisplayText_AllParts_ListsTypeIdTitleAndState()
        {
            var workItem = new ShelvesetWorkItemViewModel(123, "Fix the login", "Task", "Active", "John Smith");

            Assert.AreEqual("Task 123: Fix the login (Active)", workItem.DisplayText);
        }

        [TestMethod]
        public void DisplayText_IdOnly_ListsTheId()
        {
            var workItem = new ShelvesetWorkItemViewModel(123);

            Assert.AreEqual("123", workItem.DisplayText);
        }

        [TestMethod]
        public void DisplayText_NoState_LeavesTheStateOut()
        {
            var workItem = new ShelvesetWorkItemViewModel(123, "Fix the login", "Bug");

            Assert.AreEqual("Bug 123: Fix the login", workItem.DisplayText);
        }

        [TestMethod]
        public void Constructor_NullParts_AreEmpty()
        {
            var workItem = new ShelvesetWorkItemViewModel(123, null, null, null, null);

            Assert.AreEqual(string.Empty, workItem.Title);
            Assert.AreEqual(string.Empty, workItem.WorkItemType);
            Assert.AreEqual(string.Empty, workItem.State);
            Assert.AreEqual(string.Empty, workItem.AssignedTo);
        }
    }
}
