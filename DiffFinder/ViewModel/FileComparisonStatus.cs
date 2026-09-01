namespace DiffFinder
{
    /// <summary>
    /// How a file in the comparison summary compares to its counterpart in the other shelveset.
    /// </summary>
    public enum FileComparisonStatus
    {
        /// <summary>
        /// The file is present in both shelvesets with the same content.
        /// </summary>
        Matching,

        /// <summary>
        /// The file is present in both shelvesets but the content differs.
        /// </summary>
        Different,

        /// <summary>
        /// The file has no counterpart in the other shelveset.
        /// </summary>
        NoMatch
    }
}
