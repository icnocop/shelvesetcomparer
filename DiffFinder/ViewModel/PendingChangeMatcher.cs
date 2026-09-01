namespace DiffFinder
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// Pairs the files of two shelvesets up and decides whether the content of a pair is the same.
    /// Kept free of the Team Foundation types so that it can be exercised through <see cref="IPendingChange"/>.
    /// </summary>
    public static class PendingChangeMatcher
    {
        /// <summary>
        /// Finds the pending change in the other shelveset that corresponds to the given one. Matching is
        /// attempted by item id first, then by identical path, and finally by the longest matching relative
        /// path, which is what pairs files up when the two shelvesets are on different branches.
        /// </summary>
        /// <param name="pendingChange">The pending change to find the counterpart of</param>
        /// <param name="otherChanges">The pending changes of the other shelveset</param>
        /// <returns>The matching pending change, or null when there is none</returns>
        public static IPendingChange FindMatch(IPendingChange pendingChange, IPendingChange[] otherChanges)
        {
            if (pendingChange == null || otherChanges == null)
            {
                return null;
            }

            var matchingFile = otherChanges.FirstOrDefault(s => s.ItemId == pendingChange.ItemId);
            if (matchingFile == null)
            {
                // not matched by ItemId, try LocalOrServerItem
                matchingFile = otherChanges.FirstOrDefault(s => s.LocalOrServerItem == pendingChange.LocalOrServerItem);
            }

            if (matchingFile == null)
            {
                // still not matched, try to find a best matching file by relative path.
                matchingFile = FindMatchByRelativePath(pendingChange, otherChanges);
            }

            return matchingFile;
        }

        /// <summary>
        /// Compares the contents of the two given pending changes, by upload hash when one is available and
        /// by downloading and comparing the shelved files otherwise.
        /// </summary>
        /// <param name="firstPendingChange">The first pending change</param>
        /// <param name="secondPendingChange">The second pending change</param>
        /// <returns>True if the file contents are the same. False otherwise.</returns>
        public static bool AreContentsSame(IPendingChange firstPendingChange, IPendingChange secondPendingChange)
        {
            if (firstPendingChange != null && secondPendingChange != null
                && firstPendingChange.ChangeType != ChangeType.Delete && secondPendingChange.ChangeType != ChangeType.Delete)
            {
                if (firstPendingChange.UploadHashValue != null)
                {
                    return secondPendingChange.UploadHashValue != null
                        && firstPendingChange.UploadHashValue.SequenceEqual(secondPendingChange.UploadHashValue);
                }

                using (var firstFileStream = firstPendingChange.DownloadShelvedFile())
                using (var secondFileStream = secondPendingChange.DownloadShelvedFile())
                {
                    return StreamCompare(firstFileStream, secondFileStream);
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the pending change whose path shares the longest trailing part with the given one, which is
        /// how files are paired up when the two shelvesets come from different branches.
        /// </summary>
        /// <param name="pendingChange">The pending change to find the counterpart of</param>
        /// <param name="otherChanges">The pending changes of the other shelveset</param>
        /// <returns>The best matching pending change, or null when there is none</returns>
        private static IPendingChange FindMatchByRelativePath(IPendingChange pendingChange, IPendingChange[] otherChanges)
        {
            IPendingChange bestMatchingItem = null;
            var itemPath = pendingChange?.LocalOrServerItem;
            if (itemPath == null)
            {
                return null;
            }

            var remainingPath = Path.GetDirectoryName(itemPath).Replace('\\', '/');
            var relativeItemPath = itemPath.Replace(remainingPath + "/", string.Empty);

            do
            {
                var matches = otherChanges.Where(pc => pc.LocalOrServerItem != null && pc.LocalOrServerItem.EndsWith(relativeItemPath, StringComparison.OrdinalIgnoreCase));
                if (matches.Count() == 1)
                {
                    bestMatchingItem = matches.First();
                }
                else if (!matches.Any())
                {
                    return bestMatchingItem;
                }

                remainingPath = Path.GetDirectoryName(remainingPath).Replace('\\', '/');
                relativeItemPath = itemPath.Replace(remainingPath + "/", string.Empty);
            }
            while (remainingPath != "$" && remainingPath.Length > 0);

            return bestMatchingItem;
        }

        /// <summary>
        /// Compares two given streams.
        /// </summary>
        /// <param name="firstFileStream">The first file stream</param>
        /// <param name="secondFileStream">The second file stream</param>
        /// <returns>True if the content of the streams is the same. False otherwise</returns>
        private static bool StreamCompare(Stream firstFileStream, Stream secondFileStream)
        {
            int file1byte;
            int file2byte;

            do
            {
                file1byte = firstFileStream.ReadByte();
                file2byte = secondFileStream.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            return (file1byte - file2byte) == 0;
        }
    }
}
