using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace DiffFinder.Tests
{
    /// <summary>
    /// Loads parts of the views of the extension from their XAML, so that the tests exercise the controls
    /// the views show. The views themselves cannot be created outside of Visual Studio, so only the named
    /// element is taken, without the event handlers, which belong to the view.
    /// </summary>
    internal static class ViewXaml
    {
        private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        /// <summary>
        /// Loads the element of the given name from the given view.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="view">The file name of the view, in the Views folder of the extension</param>
        /// <param name="name">The name of the element</param>
        /// <returns>The element, with the resource dictionary of the extension</returns>
        public static T Load<T>(string view, string name)
            where T : FrameworkElement
        {
            // registers the pack:// scheme the resource dictionary is loaded through
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;

            var document = XDocument.Load(Path.Combine(FindRepositoryRoot(), "DiffFinder", "Views", view));
            var element = document.Descendants().Single(e => (string)e.Attribute("Name") == name || (string)e.Attribute(XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml") + "Name") == name);

            foreach (var descendant in element.DescendantsAndSelf())
            {
                descendant.Attributes()
                    .Where(a => !a.IsNamespaceDeclaration && IsEventHandler(descendant, a))
                    .ToList()
                    .ForEach(a => a.Remove());
            }

            // the element goes in a container holding the resources the view holds, because the resources of
            // the element itself are not there yet when its own attributes are read
            var container = new XElement(
                Presentation + "Border",
                new XElement(
                    Presentation + "Border.Resources",
                    new XElement(Presentation + "ResourceDictionary", new XAttribute("Source", "pack://application:,,,/ShelvesetComparer;component/Resources/ResourceDictionary.xaml"))),
                element);

            // the namespaces declared on the view, the local one qualified with the assembly of the extension
            foreach (var declaration in document.Root.Attributes().Where(a => a.IsNamespaceDeclaration))
            {
                container.SetAttributeValue(declaration.Name, Qualify(declaration.Value));
            }

            // the elements and attached properties of the local namespace, such as GridViewSorter.AutoSort
            foreach (var descendant in container.Descendants())
            {
                descendant.Name = XNamespace.Get(Qualify(descendant.Name.NamespaceName)) + descendant.Name.LocalName;
                foreach (var attribute in descendant.Attributes().Where(a => !a.IsNamespaceDeclaration && Qualify(a.Name.NamespaceName) != a.Name.NamespaceName).ToList())
                {
                    attribute.Remove();
                    descendant.SetAttributeValue(XNamespace.Get(Qualify(attribute.Name.NamespaceName)) + attribute.Name.LocalName, attribute.Value);
                }
            }

            return (T)((System.Windows.Controls.Border)XamlReader.Parse(container.ToString())).Child;
        }

        /// <summary>
        /// Qualifies the XAML namespace of the extension with its assembly, which the view leaves out because
        /// it is compiled into that assembly.
        /// </summary>
        private static string Qualify(string xamlNamespace)
        {
            return xamlNamespace.StartsWith("clr-namespace:DiffFinder", StringComparison.Ordinal) && !xamlNamespace.Contains(";assembly=")
                ? xamlNamespace + ";assembly=ShelvesetComparer"
                : xamlNamespace;
        }

        /// <summary>
        /// Shows the element in a window off screen, laid out and rendered.
        /// </summary>
        /// <param name="content">The element, as returned by <see cref="Load{T}"/></param>
        /// <returns>The window, to be closed by the caller</returns>
        public static Window Show(FrameworkElement content)
        {
            var window = new Window
            {
                // the container holding the resources of the element
                Content = (content.Parent as FrameworkElement) ?? content,
                Width = 1600,
                Height = 400,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.None
            };
            window.Show();
            DoEvents();
            return window;
        }

        /// <summary>
        /// Lets the dispatcher process the pending layout, rendering and data binding work.
        /// </summary>
        public static void DoEvents()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// Returns the visual descendants of the given type.
        /// </summary>
        public static IEnumerable<T> FindDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindDescendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Tells whether the attribute wires an event handler of the view.
        /// </summary>
        private static bool IsEventHandler(XElement element, XAttribute attribute)
        {
            var type = typeof(FrameworkElement).Assembly.GetType("System.Windows.Controls." + element.Name.LocalName)
                ?? typeof(FrameworkElement).Assembly.GetType("System.Windows.Controls.Primitives." + element.Name.LocalName)
                ?? typeof(FrameworkElement).Assembly.GetType("System.Windows." + element.Name.LocalName);
            return type != null && type.GetEvent(attribute.Name.LocalName) != null;
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
