using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for <see cref="UserListBuilder"/>.
    /// </summary>
    [TestClass]
    public class UserListBuilderTests
    {
        [TestMethod]
        public void Build_Null_ReturnsOnlyTheNoUserEntry()
        {
            var users = UserListBuilder.Build(null);

            CollectionAssert.AreEqual(new[] { UserListBuilder.NoUser }, users.ToArray());
        }

        [TestMethod]
        public void Build_NoNames_ReturnsOnlyTheNoUserEntry()
        {
            var users = UserListBuilder.Build(Array.Empty<string>());

            CollectionAssert.AreEqual(new[] { UserListBuilder.NoUser }, users.ToArray());
        }

        [TestMethod]
        public void Build_Names_PrependsTheNoUserEntry()
        {
            var users = UserListBuilder.Build(new[] { "John Smith" });

            Assert.AreEqual(UserListBuilder.NoUser, users[0]);
            Assert.AreEqual("John Smith", users[1]);
        }

        [TestMethod]
        public void Build_NullAndBlankNames_AreDropped()
        {
            var users = UserListBuilder.Build(new[] { "John Smith", null, string.Empty, "   " });

            CollectionAssert.AreEqual(new[] { UserListBuilder.NoUser, "John Smith" }, users.ToArray());
        }

        [TestMethod]
        public void Build_NamesDifferingOnlyInCasing_AreCollapsedIntoOneEntry()
        {
            var users = UserListBuilder.Build(new[] { "John Smith", "JOHN SMITH" });

            CollectionAssert.AreEqual(new[] { UserListBuilder.NoUser, "John Smith" }, users.ToArray());
        }

        [TestMethod]
        public void Build_SurroundingWhitespace_IsTrimmed()
        {
            var users = UserListBuilder.Build(new[] { "  John Smith  " });

            CollectionAssert.AreEqual(new[] { UserListBuilder.NoUser, "John Smith" }, users.ToArray());
        }

        [TestMethod]
        public void Build_UnsortedNames_AreSortedAlphabeticallyIgnoringCase()
        {
            var users = UserListBuilder.Build(new[] { "john smith", "Adam Ant", "Zoe Zed", "Brian Bell" });

            CollectionAssert.AreEqual(
                new[] { UserListBuilder.NoUser, "Adam Ant", "Brian Bell", "john smith", "Zoe Zed" },
                users.ToArray());
        }
    }
}
