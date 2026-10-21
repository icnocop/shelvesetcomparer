using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Tests for the VSIX manifest against the rules the Visual Studio Marketplace enforces when publishing,
    /// which would otherwise only surface when the Publish workflow runs.
    /// </summary>
    [TestClass]
    public class VsixManifestTests
    {
        /// <summary>
        /// The longest tag the Marketplace accepts (VsixPub0023)
        /// </summary>
        private const int MaximumTagLength = 50;

        /// <summary>
        /// The name of the extension on the Marketplace, as in its URL
        /// https://marketplace.visualstudio.com/items?itemName=RamiAbughazaleh.DiffFinderVS2026
        /// </summary>
        private const string MarketplaceExtensionName = "DiffFinderVS2026";

        /// <summary>
        /// The VSIX ID of the extension on the Marketplace
        /// </summary>
        private const string MarketplaceVsixId = "E81544A7-2D67-4C97-921F-2C594B3BBD06";

        private static readonly XNamespace Vsix = "http://schemas.microsoft.com/developer/vsx-schema/2011";

        [TestMethod]
        public void Tags_AreSeparatedBySemicolons()
        {
            // the manifest separates tags with semicolons, so a comma would make one long tag out of several
            foreach (var tag in ReadTags())
            {
                Assert.IsFalse(tag.Contains(","), $"The tag '{tag}' contains a comma; tags are separated by semicolons.");
            }
        }

        [TestMethod]
        public void Tags_AreNoLongerThanTheMarketplaceAccepts()
        {
            foreach (var tag in ReadTags())
            {
                Assert.IsTrue(tag.Length <= MaximumTagLength, $"The tag '{tag}' is {tag.Length} characters long; the Marketplace accepts up to {MaximumTagLength}.");
            }
        }

        [TestMethod]
        public void Tags_AreListed()
        {
            Assert.IsTrue(ReadTags().Length > 1);
        }

        [TestMethod]
        public void PublishManifest_NamesTheExtensionPublishedOnTheMarketplace()
        {
            // the Marketplace knows the extension as RamiAbughazaleh.DiffFinderVS2026. Another internal name
            // asks for a new extension, which the Marketplace refuses because the VSIX ID is already in use
            // by this one (VsixPub0029).
            var publishManifest = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "publishManifest.json"));
            var internalName = Regex.Match(publishManifest, "\"internalName\"\\s*:\\s*\"([^\"]*)\"").Groups[1].Value;

            Assert.AreEqual(MarketplaceExtensionName, internalName);
        }

        [TestMethod]
        public void Identity_IsTheOneOfTheExtensionPublishedOnTheMarketplace()
        {
            // a different VSIX ID makes a different extension, which the installed copies would not update to
            var manifest = XDocument.Load(Path.Combine(FindRepositoryRoot(), "DiffFinder", "source.extension.vsixmanifest"));
            var id = (string)manifest.Descendants(Vsix + "Identity").Single().Attribute("Id");

            Assert.AreEqual(MarketplaceVsixId, id, ignoreCase: true);
        }

        private static string[] ReadTags()
        {
            var manifest = XDocument.Load(Path.Combine(FindRepositoryRoot(), "DiffFinder", "source.extension.vsixmanifest"));
            var tags = (string)manifest.Descendants(Vsix + "Tags").Single();

            return tags.Split(';').Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToArray();
        }

        private static string FindRepositoryRoot()
        {
            for (var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "DiffFinder.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("The repository root was not found above " + AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
