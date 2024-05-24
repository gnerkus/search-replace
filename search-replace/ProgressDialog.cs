namespace search_replace
{
    // this is the modal dialog that show progress bar and the file name
    public partial class ProgressDialog : Form
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the progress of the operation being displayed in the progress dialog.
        /// </summary>
        /// <param name="filePath">The path of the current file being processed.</param>
        /// <param name="progressPercent">The percentage of completion of the operation.</param>
        /// <remarks>
        /// This method updates the text displayed for the current file being processed
        /// and adjusts the progress bar to reflect the progress percentage.
        /// The method checks if the current thread is different from the one that created the control
        /// If invoked from a different thread than the one that created the control,
        /// it will use <see cref="M:System.Windows.Forms.Control.Invoke" /> to marshal the call to the proper
        /// thread.
        /// </remarks>
        public void UpdateProgress(string filePath, int progressPercent)
        {
            if (InvokeRequired)
            {
                // UpdateProgress was called from a different thread than the thread of this control
                Invoke(
                    () => UpdateProgress(filePath, progressPercent)
                );
                return;
            }

            lblFilePath.Text = filePath;
            progressBar.Value = progressPercent;
        }

        /// <summary>
        /// called when the dialog is loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // sets the title of the dialog box
            Text = Application.ProductName;
        }
    }
}