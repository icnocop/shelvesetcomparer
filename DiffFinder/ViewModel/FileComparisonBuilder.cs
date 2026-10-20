namespace DiffFinder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Pairs the files of two shelvesets up into the rows of the comparison summary, and orders the rows.
    /// Kept free of the Team Foundation types so that it can be exercised through <see cref="IPendingChange"/>.
    /// </summary>
    public static class FileComparisonBuilder
    {
        /// <summary>
        /// Pairs the files of the two shelvesets up.
        /// </summary>
        /// <param name="firstChanges">The pending changes of the first shelveset</param>
        /// <param name="secondChanges">The pending changes of the second shelveset</param>
        /// <param name="firstShelvesetName">The name of the first shelveset</param>
        /// <param name="secondShelvesetName">The name of the second shelveset</param>
        /// <returns>The rows, the files of the first shelveset first, and how many of them match</returns>
        public static FileComparison Build(IPendingChange[] firstChanges, IPendingChange[] secondChanges, string firstShelvesetName, string secondShelvesetName)
        {
            var rows = new List<FileComparisonViewModel>();
            var listedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int sameContentFileCount = 0;
            int commonFilesCount = 0;
            foreach (var pendingChange in firstChanges)
            {
                var matchingFile = PendingChangeMatcher.FindMatch(pendingChange, secondChanges);

                bool sameContent = matchingFile != null && PendingChangeMatcher.AreContentsSame(pendingChange, matchingFile);
                rows.Add(new FileComparisonViewModel()
                {
                    FirstFile = pendingChange,
                    SecondFile = matchingFile,
                    FirstShelveName = firstShelvesetName,
                    SecondShelveName = secondShelvesetName,
                    Status = sameContent ? FileComparisonStatus.Matching : (matchingFile != null) ? FileComparisonStatus.Different : FileComparisonStatus.NoMatch
                });

                listedPaths.Add(GetListedPath(pendingChange));
                if (sameContent)
                {
                    ++sameContentFileCount;
                }

                if (matchingFile != null)
                {
                    ++commonFilesCount;
                }
            }

            foreach (var pendingChange in secondChanges)
            {
                // a file at the same path, or the same file renamed, is already listed
                if (listedPaths.Contains(GetListedPath(pendingChange))
                    || rows.Any(item => (item.FirstFile?.ItemId == pendingChange.ItemId) || (item.SecondFile?.ItemId == pendingChange.ItemId)))
                {
                    continue;
                }

                rows.Add(new FileComparisonViewModel()
                {
                    SecondFile = pendingChange,
                    SecondShelveName = secondShelvesetName,
                    Status = FileComparisonStatus.NoMatch
                });

                listedPaths.Add(GetListedPath(pendingChange));
            }

            return new FileComparison(rows.AsReadOnly(), commonFilesCount, sameContentFileCount);
        }

        /// <summary>
        /// Orders the rows of the comparison summary. The rows are ordered by the server path of their file,
        /// which is what a code review lists them by, whether or not the file is mapped locally.
        /// </summary>
        /// <param name="rows">The rows, in the order <see cref="Build"/> returned them</param>
        /// <param name="order">The order to list the rows in</param>
        /// <returns>The ordered rows</returns>
        public static IEnumerable<FileComparisonViewModel> Order(IEnumerable<FileComparisonViewModel> rows, FileOrder order)
        {
            switch (order)
            {
                case FileOrder.Tree:
                    return rows.OrderBy(GetServerPath, TreeOrderComparer.Instance);
                case FileOrder.Path:
                    return rows.OrderBy(GetServerPath, StringComparer.OrdinalIgnoreCase);
                default:
                    return rows;
            }
        }

        /// <summary>
        /// Returns the path the file of a pending change is listed with in the comparison summary.
        /// </summary>
        /// <param name="pendingChange">The pending change</param>
        /// <returns>The path</returns>
        private static string GetListedPath(IPendingChange pendingChange)
        {
            return pendingChange.LocalOrServerFolder + "/" + pendingChange.FileName;
        }

        /// <summary>
        /// Returns the server path of the file of the row, from the first shelveset when it has the file.
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The server path</returns>
        private static string GetServerPath(FileComparisonViewModel row)
        {
            var file = row.FirstFile ?? row.SecondFile;
            return file?.ServerItem ?? file?.LocalOrServerItem ?? string.Empty;
        }

        /// <summary>
        /// Orders server paths the way a folder tree lists them: folder by folder, the subfolders of a
        /// folder before its files, each ignoring case.
        /// </summary>
        private class TreeOrderComparer : IComparer<string>
        {
            /// <summary>
            /// The single instance of the comparer.
            /// </summary>
            public static readonly TreeOrderComparer Instance = new TreeOrderComparer();

            /// <inheritdoc/>
            public int Compare(string x, string y)
            {
                var first = (x ?? string.Empty).Split('/');
                var second = (y ?? string.Empty).Split('/');

                for (var i = 0; i < first.Length && i < second.Length; i++)
                {
                    var firstIsFolder = i < first.Length - 1;
                    var secondIsFolder = i < second.Length - 1;
                    if (firstIsFolder != secondIsFolder)
                    {
                        return firstIsFolder ? -1 : 1;
                    }

                    var result = StringComparer.OrdinalIgnoreCase.Compare(first[i], second[i]);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                return first.Length.CompareTo(second.Length);
            }
        }
    }
}
