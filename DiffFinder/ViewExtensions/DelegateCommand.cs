namespace DiffFinder
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// A command running the given delegates, for the buttons and column headers bound to a view model.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        /// <summary>
        /// Runs the command
        /// </summary>
        private readonly Action<object> execute;

        /// <summary>
        /// Tells whether the command can run, null when it always can
        /// </summary>
        private readonly Func<object, bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="execute">Runs the command, given the command parameter</param>
        /// <param name="canExecute">Tells whether the command can run, given the command parameter</param>
        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        /// <inheritdoc/>
        public event EventHandler CanExecuteChanged;

        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            if (this.CanExecute(parameter))
            {
                this.execute(parameter);
            }
        }

        /// <summary>
        /// Tells the controls bound to the command that whether it can run may have changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
