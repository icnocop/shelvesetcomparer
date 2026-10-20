// <copyright file="FileComparisonViewModel.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

using Microsoft.TeamFoundation.VersionControl.Client;
using System.Globalization;

namespace DiffFinder
{
    /// <summary>
    /// The view model used for each file in the comparison grid.
    /// </summary>
    public class FileComparisonViewModel
    {
        /// <summary>
        /// Gets or sets the first pending change file
        /// </summary>
        public IPendingChange FirstFile { get; set; }

        /// <summary>
        /// Gets or sets the first shelve name
        /// </summary>
        public string FirstShelveName { get; set; }

        /// <summary>
        /// Gets or sets the second pending change file
        /// </summary>
        public IPendingChange SecondFile { get; set; }

        /// <summary>
        /// Gets or sets the second shelve name
        /// </summary>
        public string SecondShelveName { get; set; }

        /// <summary>
        /// Gets the display name of the first file
        /// </summary>
        public string FirstFileDisplayName
        {
            get
            {
                return GetFullFilePath(this.FirstFile);
            }
        }

        /// <summary>
        /// Gets the display name of the second file
        /// </summary>
        public string SecondFileDisplayName
        {
            get
            {
                return GetFullFilePath(this.SecondFile);
            }
        }

        /// <summary>
        /// Gets or sets how the file compares to its counterpart in the other shelveset. The comparison
        /// summary colors the row from it, picking a color readable in the current Visual Studio theme.
        /// </summary>
        public FileComparisonStatus Status { get; set; }

        /// <summary>
        /// Gets the file on the given side of the row.
        /// </summary>
        /// <param name="side">The side of the row</param>
        /// <returns>The file, or null when the shelveset of that side has no file in this row</returns>
        public IPendingChange GetFile(ComparisonSide side)
        {
            return side == ComparisonSide.First ? this.FirstFile : this.SecondFile;
        }

        /// <summary>
        /// Gets the name of the shelveset on the given side of the row.
        /// </summary>
        /// <param name="side">The side of the row</param>
        /// <returns>The name of the shelveset</returns>
        public string GetShelveName(ComparisonSide side)
        {
            return side == ComparisonSide.First ? this.FirstShelveName : this.SecondShelveName;
        }

        /// <summary>
        /// Pairs two files the grid did not align, such as a file renamed between the shelvesets without
        /// being renamed in version control. The file of the first shelveset goes on the left like in the
        /// grid; when both files come from the same shelveset the file picked first goes on the left.
        /// </summary>
        /// <param name="source">The row of the file picked first</param>
        /// <param name="sourceSide">The side of the file picked first</param>
        /// <param name="target">The row of the file it is compared to</param>
        /// <param name="targetSide">The side of the file it is compared to</param>
        /// <returns>The pair to compare</returns>
        public static FileComparisonViewModel CreateUnaligned(FileComparisonViewModel source, ComparisonSide sourceSide, FileComparisonViewModel target, ComparisonSide targetSide)
        {
            var sourceIsLeft = sourceSide == targetSide || sourceSide == ComparisonSide.First;
            var left = sourceIsLeft ? source : target;
            var leftSide = sourceIsLeft ? sourceSide : targetSide;
            var right = sourceIsLeft ? target : source;
            var rightSide = sourceIsLeft ? targetSide : sourceSide;

            return new FileComparisonViewModel
            {
                FirstFile = left.GetFile(leftSide),
                FirstShelveName = left.GetShelveName(leftSide),
                SecondFile = right.GetFile(rightSide),
                SecondShelveName = right.GetShelveName(rightSide),
                Status = FileComparisonStatus.Different
            };
        }

        /// <summary>
        /// Returns the full file path of the pending change file.
        /// </summary>
        /// <param name="pendingChange">The pending change file</param>
        /// <returns>The full file path of the given pending change</returns>
        private static string GetFullFilePath(IPendingChange pendingChange)
        {
            return (pendingChange != null) ? string.Format(CultureInfo.CurrentCulture, @"{0}/{1}", pendingChange.LocalOrServerFolder, pendingChange.FileName) : string.Empty;
        }
    }
}
