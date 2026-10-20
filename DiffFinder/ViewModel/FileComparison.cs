namespace DiffFinder
{
    using System.Collections.Generic;

    /// <summary>
    /// The files of two shelvesets, paired up into the rows of the comparison summary.
    /// </summary>
    public class FileComparison
    {
        /// <summary>
        /// Initializes a new instance of the FileComparison class.
        /// </summary>
        /// <param name="files">The rows</param>
        /// <param name="commonFileCount">The number of files present in both shelvesets</param>
        /// <param name="matchingFileCount">The number of files present in both shelvesets with the same content</param>
        public FileComparison(IReadOnlyList<FileComparisonViewModel> files, int commonFileCount, int matchingFileCount)
        {
            this.Files = files;
            this.CommonFileCount = commonFileCount;
            this.MatchingFileCount = matchingFileCount;
        }

        /// <summary>
        /// Gets the rows: the files of the first shelveset in the order they were shelved in, then the files
        /// only the second shelveset has.
        /// </summary>
        public IReadOnlyList<FileComparisonViewModel> Files { get; private set; }

        /// <summary>
        /// Gets the number of files present in both shelvesets.
        /// </summary>
        public int CommonFileCount { get; private set; }

        /// <summary>
        /// Gets the number of files present in both shelvesets with the same content.
        /// </summary>
        public int MatchingFileCount { get; private set; }

        /// <summary>
        /// Gets the number of rows whose files do not match, including the files only one shelveset has.
        /// </summary>
        public int DifferentFileCount
        {
            get
            {
                return this.Files.Count - this.MatchingFileCount;
            }
        }
    }
}
