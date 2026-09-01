namespace DiffFinder
{
    using System;
    using System.ComponentModel;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;

    /// <summary>
    /// Supplies the colors the comparison summary uses to tell the file states apart.
    /// Visual Studio has no themed resource key carrying that meaning, so the colors are picked from the
    /// current theme: the text color for files that match, and a red and a blue readable against the
    /// current tool window background for the other two states. The colors are recalculated whenever the
    /// Visual Studio theme changes, which is why this is a bindable singleton rather than static values.
    /// Mirrors the way <c>Microsoft.TeamFoundation.Controls.WPF.TeamFoundationColors</c> is bound in
    /// Resources\ResourceDictionary.xaml.
    /// </summary>
    public class ComparisonStatusColors : INotifyPropertyChanged
    {
        /// <summary>
        /// The single instance bound to by the resource dictionary. Constructed lazily: building it eagerly
        /// would run the constructor while the color fields below are still being initialized, leaving the
        /// colors it reads from them at their default of transparent.
        /// </summary>
        private static readonly Lazy<ComparisonStatusColors> SingleInstance =
            new Lazy<ComparisonStatusColors>(() => new ComparisonStatusColors());

        /// <summary>
        /// The color of files that differ, on a light background
        /// </summary>
        private static readonly Color LightThemeDifferent = Color.FromRgb(0xC5, 0x0F, 0x1F);

        /// <summary>
        /// The color of files that differ, on a dark background
        /// </summary>
        private static readonly Color DarkThemeDifferent = Color.FromRgb(0xFF, 0x8A, 0x8A);

        /// <summary>
        /// The color of files without a counterpart, on a light background
        /// </summary>
        private static readonly Color LightThemeNoMatch = Color.FromRgb(0x00, 0x5D, 0xBA);

        /// <summary>
        /// The color of files without a counterpart, on a dark background
        /// </summary>
        private static readonly Color DarkThemeNoMatch = Color.FromRgb(0x4F, 0xC1, 0xFF);

        /// <summary>
        /// Initializes a new instance of the ComparisonStatusColors class.
        /// </summary>
        private ComparisonStatusColors()
        {
            // the colors back resources the comparison summary always needs, so a theme service which is
            // unavailable must leave usable colors behind rather than break the resource dictionary
            this.MatchingColor = Colors.Black;
            this.DifferentColor = LightThemeDifferent;
            this.NoMatchColor = LightThemeNoMatch;

            try
            {
                this.Refresh();
                VSColorTheme.ThemeChanged += this.OnThemeChanged;
            }
            catch (Exception ex)
            {
                ShelvesetComparer.Instance?.TraceOutput($"Failed to read the Visual Studio theme colors, falling back to the light theme colors: {ex}");
            }
        }

        /// <summary>
        /// Raised when the colors changed because the Visual Studio theme changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the single instance bound to by the resource dictionary.
        /// </summary>
        public static ComparisonStatusColors Instance
        {
            get
            {
                return SingleInstance.Value;
            }
        }

        /// <summary>
        /// Gets the color of files whose content is the same in both shelvesets.
        /// </summary>
        public Color MatchingColor { get; private set; }

        /// <summary>
        /// Gets the color of files present in both shelvesets but with differing content.
        /// </summary>
        public Color DifferentColor { get; private set; }

        /// <summary>
        /// Gets the color of files without a counterpart in the other shelveset.
        /// </summary>
        public Color NoMatchColor { get; private set; }

        /// <summary>
        /// Recalculates the colors from the current Visual Studio theme.
        /// </summary>
        private void Refresh()
        {
            var background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var text = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);

            this.MatchingColor = Color.FromRgb(text.R, text.G, text.B);

            // the perceived brightness of the background decides which of the two variants stays readable
            var brightness = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
            var isDark = brightness < 128.0;

            this.DifferentColor = isDark ? DarkThemeDifferent : LightThemeDifferent;
            this.NoMatchColor = isDark ? DarkThemeNoMatch : LightThemeNoMatch;
        }

        /// <summary>
        /// Event handler recalculating the colors when the Visual Studio theme changed.
        /// </summary>
        /// <param name="e">The event arguments</param>
        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            try
            {
                this.Refresh();
            }
            catch (Exception ex)
            {
                ShelvesetComparer.Instance?.TraceOutput($"Failed to read the Visual Studio theme colors: {ex}");
                return;
            }

            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(nameof(this.MatchingColor)));
                handler(this, new PropertyChangedEventArgs(nameof(this.DifferentColor)));
                handler(this, new PropertyChangedEventArgs(nameof(this.NoMatchColor)));
            }
        }
    }
}
