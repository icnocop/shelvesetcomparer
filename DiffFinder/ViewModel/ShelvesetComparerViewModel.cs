// <copyright file="ShelvesetComparerViewModel.cs" company="https://github.com/rajeevboobna/CompareShelvesets">Copyright https://github.com/rajeevboobna/CompareShelvesets. All Rights Reserved. This code released under the terms of the Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) This is sample code only, do not use in production environments.</copyright>

using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DiffFinder
{
    /// <summary>
    /// The view model for Shelveset comparison view
    /// </summary>
    public class ShelvesetComparerViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Static Instance Variable. A Singleton instance of view model is used to pass information between tool explorer window and main view.
        /// </summary>
        private static ShelvesetComparerViewModel instance = null;

        /// <summary>
        /// The summary text message for comparison
        /// </summary>
        private string summaryText;

        /// <summary>
        /// The total number of files.
        /// </summary>
        private int totalNumberOfFiles;

        /// <summary>
        /// The total number of matching files.
        /// </summary>
        private int numberOfMatchingFiles;

        /// <summary>
        /// The total number of different files.
        /// </summary>
        private int numberOfDifferentFiles;
                
        /// <summary>
        /// The service provider
        /// </summary>
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// First Shelveset Name
        /// </summary>
        private string firstShelvesetName;

        /// <summary>
        /// Second shelveset name
        /// </summary>
        private string secondShelvesetName;
        
        /// <summary>
        /// The collection of files
        /// </summary>
        private readonly ObservableCollection<FileComparisonViewModel> files;

        /// <summary>
        /// The filter of files
        /// </summary>
        private string filter;

        /// <summary>
        /// The order the files are listed in
        /// </summary>
        private FileOrder fileOrder;

        /// <summary>
        /// Initializes a new instance of the ShelvesetComparerViewModel class
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        public ShelvesetComparerViewModel([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.files = new ObservableCollection<FileComparisonViewModel>();
            this.serviceProvider = serviceProvider;
            this.summaryText = string.Empty;
            this.totalNumberOfFiles = 0;
            this.numberOfMatchingFiles = 0;
            this.numberOfDifferentFiles = 0;
            this.firstShelvesetName = string.Empty;
            this.secondShelvesetName = string.Empty;
            this.filter = string.Empty;
        }

        /// <summary>
        /// Notification event used by view to update itself when any property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the single instance of the View Model
        /// </summary>
        public static ShelvesetComparerViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateInstanceAsync().GetResultNoContext();
                }

                return instance;
            }
        }

        private static async System.Threading.Tasks.Task CreateInstanceAsync()
        {
            if (! ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            var dte2 = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            var serviceProvider = new ServiceProvider(dte2.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            instance = new ShelvesetComparerViewModel(serviceProvider);
        }

        /// <summary>
        /// Gets or sets the summary of the comparison.
        /// </summary>
        public string SummaryText 
        {
            get
            {
                return this.summaryText;
            }

            set
            {
                this.summaryText = value;
                this.NotifyPropertyChanged("SummaryText");
            }
        }
        
        /// <summary>
        /// Gets or sets the filter for files to be shown
        /// </summary>
        public string Filter 
        {
            get
            {
                return this.filter;
            }  

            set
            {
                this.filter = value;
                this.NotifyPropertyChanged("Files");
            }
        }
        
        /// <summary>
        /// Gets or sets the order the files are listed in. Kept for as long as Visual Studio runs, so that
        /// the next comparison lists its files the same way.
        /// </summary>
        public FileOrder FileOrder
        {
            get
            {
                return this.fileOrder;
            }

            set
            {
                if (this.fileOrder == value)
                {
                    return;
                }

                this.fileOrder = value;
                this.NotifyPropertyChanged(nameof(this.FileOrder));
                this.NotifyPropertyChanged(nameof(this.Files));
            }
        }

        /// <summary>
        /// Gets or sets the total number of files
        /// </summary>
        public int TotalNumberOfFiles
        {
            get
            {
                return this.totalNumberOfFiles;
            }

            set
            {
                this.totalNumberOfFiles = value;
                this.NotifyPropertyChanged("TotalNumberOfFiles");
            }
        }

        /// <summary>
        /// Gets or sets the number of matching files
        /// </summary>
        public int NumberOfMatchingFiles 
        {
            get
            {
                return this.numberOfMatchingFiles;
            }

            set
            {
                this.numberOfMatchingFiles = value;
                this.NotifyPropertyChanged("NumberOfMatchingFiles");
            }
        }

        /// <summary>
        /// Gets or sets the number of different files
        /// </summary>
        public int NumberOfDifferentFiles
        {
            get
            {
                return this.numberOfDifferentFiles;
            }

            set
            {
                this.numberOfDifferentFiles = value;
                this.NotifyPropertyChanged("NumberOfDifferentFiles");
            }
        }

        /// <summary>
        /// Gets or sets the first shelveset name
        /// </summary>
        public string FirstShelvesetName 
        {
            get
            {
                return this.firstShelvesetName;
            }

            set
            {
                this.firstShelvesetName = value;
                this.NotifyPropertyChanged("FirstShelvesetName");
            }
        }

        /// <summary>
        /// Gets or sets the second shelveset name
        /// </summary>
        public string SecondShelvesetName 
        {
            get
            {
                return this.secondShelvesetName;
            }

            set
            {
                this.secondShelvesetName = value;
                this.NotifyPropertyChanged("SecondShelvesetName");
            }
        }
        
        /// <summary>
        /// Gets Files view model of all matching and different files.
        /// </summary>
        public IEnumerable<FileComparisonViewModel> Files
        {
            get
            {
                var ordered = FileComparisonBuilder.Order(this.files, this.fileOrder);
                if (string.IsNullOrWhiteSpace(this.filter))
                {
                    return ordered.ToList();
                }
                else
                {
                    return ordered.Where(s => HasMatchingFileName(s, this.filter)).ToList();
                }
            }
        }

        /// <summary>
        /// Replaces the files listed.
        /// </summary>
        /// <param name="comparedFiles">The files, in the order they were shelved in</param>
        public void SetFiles(IEnumerable<FileComparisonViewModel> comparedFiles)
        {
            this.files.Clear();
            foreach (var item in comparedFiles ?? Enumerable.Empty<FileComparisonViewModel>())
            {
                this.files.Add(item);
            }

            this.NotifyPropertyChanged(nameof(this.Files));
        }

        //public void Initialize(ShelvesetViewModel firstShelveset, ShelvesetViewModel secondShelveset)
        //{
        //    Initialize(firstShelveset?.Shelveset, secondShelveset?.Shelveset);
        //}

        /// <summary>
        /// Initializes the Shelveset Comparison View Model
        /// </summary>
        /// <param name="firstShelveset">The first shelveset.</param>
        /// <param name="secondShelveset">The second shelveset</param>
        public void Initialize(ShelvesetViewModel firstShelveset, ShelvesetViewModel secondShelveset)
        {
            if (firstShelveset == null)
            {
                throw new ArgumentNullException("firstShelveset");
            }

            if (secondShelveset == null)
            {
                throw new ArgumentNullException("secondShelveset");
            }

            var tfcontextManager = this.GetService<ITeamFoundationContextManager>();
            if (tfcontextManager == null)
            {
                this.SummaryText = Resources.ConnectionErrorMessage + " (no ITeamFoundationContextManager)";
                return;
            }

            var vcs = tfcontextManager.CurrentContext?.TeamProjectCollection?.GetService<VersionControlServer>();
            if (vcs == null)
            {
                this.SummaryText = Resources.ConnectionErrorMessage + " (no VersionControlServer)";
                return;
            }

            this.FirstShelvesetName = firstShelveset.Name;
            this.SecondShelvesetName = secondShelveset.Name;

            this.SetFiles(null);
            var firstShelvesetChanges = GetPendingChanges(firstShelveset, vcs);
            if (firstShelvesetChanges == null)
            {
                this.SummaryText = "Failed to get pending changes for first shelveset: " + FirstShelvesetName;
                return;
            }
            var secondShelvesetChanges = GetPendingChanges(secondShelveset, vcs);
            if (secondShelvesetChanges == null)
            {
                this.SummaryText = "Failed to get pending changes for second shelveset: " + SecondShelvesetName;
                return;
            }

            var comparison = FileComparisonBuilder.Build(firstShelvesetChanges, secondShelvesetChanges, this.FirstShelvesetName, this.SecondShelvesetName);
            this.SetFiles(comparison.Files);

            if (firstShelveset.Name == secondShelveset.Name && firstShelveset.OwnerName == secondShelveset.OwnerName)
            {
                this.SummaryText = Resources.SameShelvesetMessage;
                this.TotalNumberOfFiles = firstShelvesetChanges.Count();
                this.NumberOfDifferentFiles = 0;
                this.NumberOfMatchingFiles = firstShelvesetChanges.Count();
            }
            else
            {
                this.SummaryText = string.Format(CultureInfo.CurrentCulture, Resources.SummaryMessage, comparison.CommonFileCount, comparison.MatchingFileCount, comparison.DifferentFileCount);
                this.TotalNumberOfFiles = comparison.CommonFileCount;
                this.NumberOfMatchingFiles = comparison.MatchingFileCount;
                this.NumberOfDifferentFiles = comparison.DifferentFileCount;
            }
        }

        /// <summary>
        /// Get the pending changes shelved in the given shelveset.
        /// </summary>
        /// <param name="shelveset">The shelveset to read the pending changes of</param>
        /// <param name="vcs">The version control server to read from</param>
        /// <returns>The pending changes of the shelveset</returns>
        private IPendingChange[] GetPendingChanges(ShelvesetViewModel shelveset, VersionControlServer vcs)
        {
            return vcs.QueryShelvedChanges(shelveset.Shelveset)[0]
                ?.PendingChanges
                ?.Select(pc => new PendingChangeFacade(pc))
                ?.ToArray<IPendingChange>();
        }

        /// <summary>
        /// Returns true or false depending upon whether the first or second file name starts with the given filter.
        /// </summary>
        /// <param name="fileComparisonViewModel">The file comparison object to looking into</param>
        /// <param name="filter">The filter</param>
        /// <returns>True if the name exists. False otherwise</returns>
        private static bool HasMatchingFileName(FileComparisonViewModel fileComparisonViewModel, string filter)
        {
            return fileComparisonViewModel.FirstFileDisplayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                fileComparisonViewModel.SecondFileDisplayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns the service of the given type.
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>The service.</returns>
        private T GetService<T>()
        {
            if (this.serviceProvider != null)
            {
                return (T)this.serviceProvider.GetService(typeof(T));
            }

            return default;
        }

        /// <summary>
        /// The method raise the Property Changed event for the given property
        /// </summary>
        /// <param name="propertyName">The property for which the event needs to be raised</param>
        private void NotifyPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
