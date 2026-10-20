namespace DiffFinder
{
    /// <summary>
    /// The order the files of the comparison summary are listed in.
    /// </summary>
    public enum FileOrder
    {
        /// <summary>
        /// By server path, listing the subfolders of a folder before its files, like the folder tree of a
        /// code review.
        /// </summary>
        Tree,

        /// <summary>
        /// By full server path, like the flat list of a code review.
        /// </summary>
        Path,

        /// <summary>
        /// In the order the files were shelved in: the files of the first shelveset, then the files only
        /// the second shelveset has.
        /// </summary>
        Shelveset
    }
}
