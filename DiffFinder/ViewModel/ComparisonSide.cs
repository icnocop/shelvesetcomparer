// <copyright file="ComparisonSide.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

namespace DiffFinder
{
    /// <summary>
    /// The shelveset, and so the column of the comparison grid, a file belongs to.
    /// </summary>
    public enum ComparisonSide
    {
        /// <summary>
        /// The first shelveset, listed in the left column.
        /// </summary>
        First,

        /// <summary>
        /// The second shelveset, listed in the right column.
        /// </summary>
        Second
    }
}
