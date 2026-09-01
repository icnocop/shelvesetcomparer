// <copyright file="SelectShelvesetSection.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DiffFinder
{
    /// <summary>
    /// The class creates the team explorer section for the Shelveset Comparer extension.
    /// </summary>
    [TeamExplorerSection("1555C86B-9D88-4AA6-9B85-99D97710BD74", ShelvesetComparerPage.PageId, 20)]
    public class SelectShelvesetSection : TeamExplorerBaseSection
    {
        /// <summary>
        /// The name of the collection level group every user with access to the collection belongs to
        /// </summary>
        private const string ValidUsersGroupName = "Project Collection Valid Users";

        /// <summary>
        /// Contains the shelveset list
        /// </summary>
        private ObservableCollection<ShelvesetViewModel> shelvesets;

        /// <summary>
        /// Contains the list of users offered by the shelveset owner drop down lists
        /// </summary>
        private ObservableCollection<string> users;

        /// <summary>
        /// Contains the user account name for the first shelveset
        /// </summary>
        private string firstUserAccountName;

        /// <summary>
        /// Contains the user account name for the second shelveset
        /// </summary>
        private string secondUserAccountName;

        /// <summary>
        /// Initializes a new instance of the SelectShelvesetSection class.
        /// </summary>
        public SelectShelvesetSection()
        {
            this.Title = Resources.TeamExplorerLinkCaption;
            this.firstUserAccountName = UserListBuilder.NoUser;
            this.secondUserAccountName = UserListBuilder.NoUser;
            this.IsVisible = true;
            this.IsExpanded = true;
            this.IsBusy = false;
            this.shelvesets = new ObservableCollection<ShelvesetViewModel>();
            this.users = new ObservableCollection<string>(UserListBuilder.Build(null));
            this.SectionContent = new SelectShelvesetTeamExplorerView(this);
        }

        /// <summary>
        /// Gets or sets the user account name for first shelveset.
        /// </summary>
        public string FirstUserAccountName
        {
            get
            {
                return this.firstUserAccountName;
            }

            set
            {
                this.firstUserAccountName = value;
                this.RaisePropertyChanged(nameof(FirstUserAccountName));
                this.CaptureState();
            }
        }

        /// <summary>
        /// Gets or sets the user account name for second shelveset.
        /// </summary>
        public string SecondUserAccountName
        {
            get
            {
                return this.secondUserAccountName;
            }

            set
            {
                this.secondUserAccountName = value;
                this.RaisePropertyChanged(nameof(SecondUserAccountName));
                this.CaptureState();
            }
        }

        /// <summary>
        /// Gets or sets the list of users offered by the shelveset owner drop down lists
        /// </summary>
        public ObservableCollection<string> Users
        {
            get
            {
                return this.users;
            }

            protected set
            {
                this.users = value;
                this.RaisePropertyChanged(nameof(Users));
                this.CaptureState();
            }
        }

        /// <summary>
        /// Gets or sets the shelveset list
        /// </summary>
        public ObservableCollection<ShelvesetViewModel> Shelvesets
        {
            get
            {
                return this.shelvesets;
            }

            protected set
            {
                this.shelvesets = value;
                this.RaisePropertyChanged(nameof(Shelvesets));
                this.CaptureState();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list of users is currently being refreshed. While it is, the
        /// selection of a shelveset owner drop down list changes because the list is being replaced and the
        /// signed in user preselected, not because the user picked somebody, so no shelveset refresh is due.
        /// </summary>
        public bool IsRefreshingUsers { get; private set; }

        /// <summary>
        /// Gets Team Foundation Context of the Team Explorer window.
        /// </summary>
        public ITeamFoundationContext Context
        {
            get
            {
                return this.CurrentContext;
            }
        }

        /// <summary>
        /// Gets the view of the current Team Explorer section
        /// </summary>
        protected SelectShelvesetTeamExplorerView View
        {
            get 
            { 
                return this.SectionContent as SelectShelvesetTeamExplorerView; 
            }
        }

        /// <summary>
        /// Overridden method that initializes the team explorer section
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Exceptions handled in method")]
        public async override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            try
            {
                base.Initialize(sender, e);

                // Team Explorer passes the saved context back on some navigations only, so fall back to the
                // state the section kept for itself. Restoring it is what lets navigating away to a
                // shelveset and back keep the listed shelvesets, the selected users and the selected rows.
                var saved = (e.Context as ShelvesetsContext) ?? ShelvesetsContext.Current;
                if (saved != null && saved.Shelvesets != null && saved.ContextKey == this.BuildContextKey())
                {
                    this.Restore(saved);
                    return;
                }

                await this.RefreshUsersAsync();
                await this.RefreshAsync();
            }
            catch (Exception)
            {
                ShowFailed();
            }
        }

        /// <summary>
        /// Refresh override.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Exceptions handled in method")]
        public override async void Refresh()
        {
            try
            {
                base.Refresh();
                await this.RefreshAsync();
            } 
            catch (Exception)
            {
                ShowFailed();
            }
        }

        /// <summary>
        /// Save the current state of the section
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);
            if (e != null)
            {
                e.Context = this.CaptureState();
            }
        }

        /// <summary>
        /// Identifies the team project collection and team project the current state belongs to, so that
        /// state captured against a different one is not restored over it.
        /// </summary>
        /// <returns>The key of the current context, or null when there is no context</returns>
        private string BuildContextKey()
        {
            try
            {
                var context = this.CurrentContext;
                if (context == null || !context.HasCollection)
                {
                    return null;
                }

                return $"{context.TeamProjectCollection.Uri}|{context.TeamProjectName}";
            }
            catch (Exception)
            {
                // the context is not reachable yet, so there is nothing to key the state on
                return null;
            }
        }

        /// <summary>
        /// Records the current state of the section so that it can be restored the next time the section is
        /// created. Called whenever a part of that state changes rather than only from
        /// <see cref="SaveContext"/>, because Team Explorer does not always ask for the context before the
        /// section goes away.
        /// </summary>
        /// <returns>The recorded state</returns>
        private ShelvesetsContext CaptureState()
        {
            var state = new ShelvesetsContext
            {
                ContextKey = this.BuildContextKey(),
                Shelvesets = this.Shelvesets,
                Users = this.Users,
                FirstUserAccountName = this.FirstUserAccountName,
                SecondUserAccountName = this.SecondUserAccountName
            };

            if (state.ContextKey != null)
            {
                ShelvesetsContext.Current = state;
            }

            return state;
        }

        /// <summary>
        /// Restores the state the section was left in. The shelveset items carry their own selected flag, so
        /// putting the same items back also puts the selected rows back.
        /// </summary>
        /// <param name="saved">The saved state of the section</param>
        private void Restore(ShelvesetsContext saved)
        {
            try
            {
                // assigning the lists clears the selection of the drop down lists, which the two way
                // binding writes back, so the selection has to be restored afterwards
                this.IsRefreshingUsers = true;

                if (saved.Users != null && saved.Users.Count > 0)
                {
                    this.Users = saved.Users;
                }

                this.FirstUserAccountName = this.Users.Contains(saved.FirstUserAccountName ?? UserListBuilder.NoUser)
                    ? saved.FirstUserAccountName
                    : UserListBuilder.NoUser;
                this.SecondUserAccountName = this.Users.Contains(saved.SecondUserAccountName ?? UserListBuilder.NoUser)
                    ? saved.SecondUserAccountName
                    : UserListBuilder.NoUser;

                this.Shelvesets = saved.Shelvesets;
            }
            finally
            {
                this.IsRefreshingUsers = false;
            }
        }

        /// <summary>
        /// Refresh the list of shelveset shelveset asynchronously.
        /// </summary>
        /// <returns>The Task doing the refresh. Needed for Async methods</returns>
        private async System.Threading.Tasks.Task RefreshShelvesetsAsync()
        {
            var firstUser = this.FirstUserAccountName;
            var secondUser = this.SecondUserAccountName;
            var context = this.CurrentContext;

            // Make the server call asynchronously to avoid blocking the UI
            var fetchShelvesetsTask = Task.Run(() => FetchShevlesets(firstUser, secondUser, context));

            this.Shelvesets = await fetchShelvesetsTask;
        }

        /// <summary>
        /// Opens up the shelveset details page for the given shelveset
        /// </summary>
        /// <param name="shelveset">The shelveset to be displayed.</param>
        public void ViewShelvesetDetails(Shelveset shelveset)
        {
            TeamExplorer.NavigateToShelvesetDetails(shelveset);
        }

        /// <summary>
        /// the method is invoked when the context of the current team explorer window has changed.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Exceptions handled in method")]
        protected override async void ContextChanged(object sender, ContextChangedEventArgs e)
        {
            try
            {
                base.ContextChanged(sender, e);

                // If the team project collection or team project changed, refresh the data for this section
                if (e.TeamProjectCollectionChanged || e.TeamProjectChanged)
                {
                    await this.RefreshUsersAsync();
                    await this.RefreshAsync();
                }
            } 
            catch (Exception)
            {
                ShowFailed();
            }
        }

        /// <summary>
        /// Retrieves the users of the team project collection and the display name of the signed in user.
        /// </summary>
        /// <param name="context">The Team foundation server context</param>
        /// <returns>The users to offer in the shelveset owner drop down lists</returns>
        private static UserList FetchUsers(ITeamFoundationContext context)
        {
            if (context == null || !context.HasCollection)
            {
                return new UserList(UserListBuilder.Build(null), UserListBuilder.NoUser);
            }

            var identityManagementService = context.TeamProjectCollection.GetService<IIdentityManagementService>();
            if (identityManagementService == null)
            {
                return new UserList(UserListBuilder.Build(null), UserListBuilder.NoUser);
            }

            // the group contains every user with access to the collection, expanded so that the members
            // of the groups it contains are returned as well
            var validUsers = identityManagementService.ReadIdentity(
                IdentitySearchFactor.AccountName,
                ValidUsersGroupName,
                MembershipQuery.ExpandedDown,
                ReadIdentityOptions.None);

            var displayNames = Enumerable.Empty<string>();
            if (validUsers != null && validUsers.Members != null)
            {
                displayNames = identityManagementService
                    .ReadIdentities(validUsers.Members, MembershipQuery.None, ReadIdentityOptions.None)
                    .Where(identity => identity != null && !identity.IsContainer && identity.IsActive)
                    .Select(identity => identity.DisplayName);
            }

            return new UserList(UserListBuilder.Build(displayNames), FetchCurrentUserDisplayName(context, identityManagementService));
        }

        /// <summary>
        /// Retrieves the display name of the signed in user, which is the entry preselected in the first
        /// shelveset owner drop down list.
        /// </summary>
        /// <param name="context">The Team foundation server context</param>
        /// <param name="identityManagementService">The identity management service of the collection</param>
        /// <returns>The display name of the signed in user, or an empty string when it cannot be resolved</returns>
        private static string FetchCurrentUserDisplayName(ITeamFoundationContext context, IIdentityManagementService identityManagementService)
        {
            var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
            if (vcs == null || string.IsNullOrWhiteSpace(vcs.AuthorizedUser))
            {
                return UserListBuilder.NoUser;
            }

            var identity = identityManagementService.ReadIdentity(
                IdentitySearchFactor.AccountName,
                vcs.AuthorizedUser,
                MembershipQuery.None,
                ReadIdentityOptions.None);

            // depending on the server version AuthorizedUser is already the display name, so it is the
            // fallback when it cannot be resolved as an account name
            return identity == null ? vcs.AuthorizedUser : identity.DisplayName;
        }

        /// <summary>
        /// Refresh the list of users offered by the shelveset owner drop down lists asynchronously.
        /// </summary>
        /// <returns>The Task doing the refresh. Needed for Async methods</returns>
        public async System.Threading.Tasks.Task RefreshUsersAsync()
        {
            try
            {
                this.IsBusy = true;
                this.IsRefreshingUsers = true;

                var context = this.CurrentContext;
                var previousFirstUser = this.FirstUserAccountName ?? UserListBuilder.NoUser;
                var previousSecondUser = this.SecondUserAccountName ?? UserListBuilder.NoUser;

                // Make the server call asynchronously to avoid blocking the UI
                var fetchUsersTask = Task.Run(() => FetchUsers(context));

                var fetchedUsers = await fetchUsersTask;

                this.Users = new ObservableCollection<string>(fetchedUsers.Users);

                // replacing the list clears the selection of the drop down lists, which the two way
                // binding writes back, so the previous selection has to be restored
                this.FirstUserAccountName = this.Users.Contains(previousFirstUser) ? previousFirstUser : UserListBuilder.NoUser;
                this.SecondUserAccountName = this.Users.Contains(previousSecondUser) ? previousSecondUser : UserListBuilder.NoUser;

                // preselect the signed in user, otherwise the first drop down list would open blank
                if (string.IsNullOrWhiteSpace(this.FirstUserAccountName))
                {
                    var currentUser = this.Users.FirstOrDefault(user =>
                        !string.IsNullOrWhiteSpace(user)
                        && string.Equals(user, fetchedUsers.CurrentUserDisplayName, StringComparison.CurrentCultureIgnoreCase));

                    if (currentUser != null)
                    {
                        this.FirstUserAccountName = currentUser;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowNotification(ex.Message, NotificationType.Error);
            }
            finally
            {
                this.IsRefreshingUsers = false;
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Retrieves the shelveset list for the current user
        /// </summary>
        /// <param name="userName">The user name </param>
        /// <param name="secondUsername">The second user name </param>
        /// <param name="context">The Team foundation server context</param>
        /// <param name="shelveSets">The shelveset list to be returned</param>
        private static ObservableCollection<ShelvesetViewModel> FetchShevlesets(string userName, string secondUsername, ITeamFoundationContext context)
        {
            var shelveSets = new ObservableCollection<ShelvesetViewModel>();
            if (context != null && context.HasCollection && context.HasTeamProject)
            {
                var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                if (vcs != null)
                {
                    string user = string.IsNullOrWhiteSpace(userName) ? vcs.AuthorizedUser : userName;
                    foreach (var shelveSet in vcs.QueryShelvesets(null, user).OrderByDescending(s => s.CreationDate))
                    {
                        shelveSets.Add(new ShelvesetViewModel(shelveSet));
                    }

                    if (!string.IsNullOrWhiteSpace(secondUsername) && secondUsername != userName)
                    {
                        user = string.IsNullOrWhiteSpace(secondUsername) ? vcs.AuthorizedUser : secondUsername;
                        foreach (var shelveSet in vcs.QueryShelvesets(null, user).OrderByDescending(s => s.CreationDate))
                        {
                            shelveSets.Add(new ShelvesetViewModel(shelveSet));
                        }
                    }
                }
            }

            return shelveSets;
        }

        /// <summary>
        /// Retrieves the shelveset for pending change for the current user 
        /// </summary>
        /// <param name="context">The Team foundation server context</param>
        internal ShelvesetViewModel FetchPendingChangeShelveset(ITeamFoundationContext context, Workspace ws = null)
        {
            if (context != null && context.HasCollection && context.HasTeamProject)
            {
                var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                if (vcs != null)
                {
                    var workspace = ws;
                    if (workspace == null)
                    {
                        var pendingChangesService = GetService<IPendingChangesExt>();
                        if (pendingChangesService != null)
                        {
                            workspace = pendingChangesService.Workspace;
                        }
                    }
                    if (workspace == null)
                    {
                        var machineName = Environment.MachineName;
                        var currentUserName = Environment.UserName;
                        workspace = vcs.GetWorkspace(machineName, currentUserName);
                    }

                    var changes = workspace.GetPendingChanges();//we want to shelve all pending changes in the workspace

                    if (changes.Length != 0)
                    {

                        var pendChange = new Shelveset(vcs, "Pending Changes", workspace.OwnerName);
                        workspace.Shelve(pendChange, changes, ShelvingOptions.Replace);//you can specify to replace existing shelveset, or to remove pending changes from the local workspace with ShelvingOptions
                        pendChange.CreationDate = DateTime.Now;

                        return new ShelvesetViewModel(pendChange);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Refresh the list of shelveset and comparison shelveset asynchronously.
        /// </summary>
        /// <returns>The Task doing the refresh. Needed for Async methods</returns>
        public async System.Threading.Tasks.Task RefreshAsync()
        {
            try
            {
                this.IsBusy = true;

                await this.RefreshShelvesetsAsync();
            }
            catch (Exception ex)
            {
                this.ShowNotification(ex.Message, NotificationType.Error);
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        private void ShowFailed([CallerMemberName] string caller = null)
        {
            this.ShowNotification($"Failed to {caller}", NotificationType.Error);
        }


        /// <summary>
        /// The result of a user list refresh.
        /// </summary>
        private class UserList
        {
            /// <summary>
            /// Initializes a new instance of the UserList class.
            /// </summary>
            /// <param name="users">The users to offer in the shelveset owner drop down lists</param>
            /// <param name="currentUserDisplayName">The display name of the signed in user</param>
            public UserList(IReadOnlyList<string> users, string currentUserDisplayName)
            {
                this.Users = users;
                this.CurrentUserDisplayName = currentUserDisplayName;
            }

            /// <summary>
            /// Gets the users to offer in the shelveset owner drop down lists.
            /// </summary>
            public IReadOnlyList<string> Users { get; private set; }

            /// <summary>
            /// Gets the display name of the signed in user.
            /// </summary>
            public string CurrentUserDisplayName { get; private set; }
        }
    }
}
