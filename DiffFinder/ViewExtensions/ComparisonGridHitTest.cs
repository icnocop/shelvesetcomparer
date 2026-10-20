namespace DiffFinder
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Tells which part of the comparison grid a mouse event happened on, from the element the event
    /// originated from.
    /// </summary>
    public static class ComparisonGridHitTest
    {
        /// <summary>
        /// Finds the cell of the comparison grid the given element is part of.
        /// </summary>
        /// <param name="element">The element, typically the source of a mouse event</param>
        /// <param name="row">The row of the cell</param>
        /// <param name="side">The side of the cell</param>
        /// <returns>True when the element is part of a cell, false otherwise</returns>
        public static bool TryGetCell(DependencyObject element, out FileComparisonViewModel row, out ComparisonSide side)
        {
            for (var current = element; current != null; current = GetParent(current))
            {
                if (current is FrameworkElement cell && cell.Tag is ComparisonSide cellSide && cell.DataContext is FileComparisonViewModel cellRow)
                {
                    row = cellRow;
                    side = cellSide;
                    return true;
                }

                if (current is ListViewItem)
                {
                    break;
                }
            }

            row = null;
            side = ComparisonSide.First;
            return false;
        }

        /// <summary>
        /// Tells whether the given element is part of a row of the grid. The column headers, including the
        /// splitters between them that a double click auto sizes a column with, are not.
        /// </summary>
        /// <param name="element">The element, typically the source of a mouse event</param>
        /// <returns>True when the element is part of a row</returns>
        public static bool IsInRow(DependencyObject element)
        {
            for (var current = element; current != null; current = GetParent(current))
            {
                if (current is ListViewItem)
                {
                    return true;
                }

                if (current is GridViewColumnHeader || current is ListView)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the parent of the element, walking up through text elements such as a Run, which are not
        /// part of the visual tree.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The parent, or null at the root</returns>
        private static DependencyObject GetParent(DependencyObject element)
        {
            return element is Visual || element is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }
    }
}
