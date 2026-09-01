using System;
using System.Collections.Generic;
using System.Linq;

namespace DiffFinder
{
    /// <summary>
    /// Builds the list of user names offered by the shelveset owner drop down lists.
    /// </summary>
    public static class UserListBuilder
    {
        /// <summary>
        /// The entry representing "no user selected". It is always the first entry so that a drop down
        /// list which does not accept free text can still be cleared. An empty first user falls back to
        /// the authorized user when the shelvesets are queried.
        /// </summary>
        public const string NoUser = "";

        /// <summary>
        /// Turns the display names read from the server into the list bound to the drop down lists:
        /// blank names are dropped, names differing only in casing are collapsed into one entry, the
        /// remaining names are sorted alphabetically and the "no user selected" entry is prepended.
        /// </summary>
        /// <param name="displayNames">The display names read from the server. May be null.</param>
        /// <returns>The list of entries to bind, never null and never empty.</returns>
        public static IReadOnlyList<string> Build(IEnumerable<string> displayNames)
        {
            var users = new List<string> { NoUser };

            if (displayNames != null)
            {
                users.AddRange(displayNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase));
            }

            return users;
        }
    }
}
