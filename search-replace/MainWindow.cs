using System.IO.MemoryMappedFiles;
using System.Text;
using Newtonsoft.Json;

namespace search_replace
{
    public partial class MainWindow : Form
    {
        private static readonly string[] FileFilterList =
            { @"\.git\", @"\.vs\", @"\packages\", @"\bin\", @"\obj\" };

        private readonly AppConfig? _appConfig;

        /// <summary>
        ///     The fully-qualified pathname to the application configuration file
        /// </summary>
        /// <remarks>
        ///     the location of the application configuration file on the file system
        /// </remarks>
        private readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ), "Nanotome", "FileReplacer", "config.json"
        );

        public MainWindow()
        {
            InitializeComponent();

            _appConfig = LoadConfig();
            UpdateTextBoxesFromConfig();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            SaveConfig();
        }

        private AppConfig? LoadConfig()
        {
            AppConfig? result;

            try
            {
                if (!File.Exists(_configFilePath))

                    // If config file doesn't exist, return a new instance
                    return new AppConfig();

                // Load config from JSON file
                var json = File.ReadAllText(_configFilePath);
                result = JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch (Exception ex)
            {
                // display an alert with the exception text
                MessageBox.Show(
                    this, ex.Message, Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Stop
                );

                result = default;
            }

            return result;
        }

        /// <summary>
        ///     Event handler for the <see cref="E:System.Windows.Forms.Control.Click" /> event
        ///     event of the <b>Browse</b> button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///     This method is called when the user clicks the <b>Browse</b> button to
        ///     select a directory using the folder browser dialog. It updates the text of the
        ///     directory path textbox with the selected directory path.
        /// </remarks>
        private void OnClickBrowseButton(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.OK &&
                !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
                txtDirectoryPath.Text = folderBrowserDialog1.SelectedPath;
        }

        /// <summary>
        ///     Event handler for the <see cref="E:System.Windows.Forms.Control.Click" /> event
        ///     of the <b>Do It</b> button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///     This method is called when the user clicks the <b>Do It</b> button
        ///     to initiate the text replacement process. It performs validation,
        ///     creates a progress dialog, starts a background task for text replacement,
        ///     and displays the progress dialog modally.
        /// </remarks>
        private void OnClickDoItButton(object sender, EventArgs e)
        {
            var directoryPath = txtDirectoryPath.Text.Trim();
            var searchText = txtSearchText.Text.Trim();
            var replaceText = txtReplaceText.Text.Trim();

            // validation
            if (string.IsNullOrEmpty(directoryPath) ||
                !Directory.Exists(directoryPath))
            {
                MessageBox.Show(
                    "Please select a valid directory.", Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show(
                    "Please type in some text to find.",
                    Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            if (string.IsNullOrEmpty(replaceText))
            {
                MessageBox.Show(
                    "Please type in some text to replace the found text with.",
                    Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            using var progressDialog = new ProgressDialog();
            // Use Progress<T> to report progress
            var progressReporter = new Progress<ProgressReport>(
                report =>
                {
                    progressDialog.UpdateProgress(
                        report.CurrentFile, report.ProgressPercent
                    );
                }
            );
            
            // Start a new task for text replacement
            Task.Run(
                () =>
                {
                    ReplaceTextInFiles(
                        directoryPath, searchText, replaceText,
                        progressReporter
                    );

                    // close the progress dialog
                    if (InvokeRequired)
                        progressDialog.BeginInvoke(
                            new MethodInvoker(progressDialog.Close)
                        );
                    else
                        progressDialog.Close();

                    // Show completion message
                    MessageBox.Show(
                        "Text replacement completed.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                }
            );

            progressDialog.ShowDialog(this);
        }

        /// <summary>
        ///     Event handler for the
        ///     <see cref="E:System.Windows.Forms.Control.TextChanged" /> event of the
        ///     <b>Starting Folder</b> text box. Updates the
        ///     <see cref="P:TextReplacementApp.AppConfig.DirectoryPath" /> property with the
        ///     trimmed text from the directory path text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void OnTextChangedDirectoryPath(object sender, EventArgs e)
        {
            if (_appConfig != null) _appConfig.DirectoryPath = txtDirectoryPath.Text.Trim();
        }

        /// <summary>
        ///     Event handler for the
        ///     <see cref="E:System.Windows.Forms.Control.TextChanged" /> event of the
        ///     <b>Replace With</b> text box. Updates the
        ///     <see cref="P:TextReplacementApp.AppConfig.ReplaceWith" /> property with the
        ///     trimmed text from the directory path text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void OnTextChangedReplaceText(object sender, EventArgs e)
        {
            if (_appConfig != null) _appConfig.ReplaceWith = txtReplaceText.Text.Trim();
        }

        /// <summary>
        ///     Event handler for the
        ///     <see cref="E:System.Windows.Forms.Control.TextChanged" /> event of the
        ///     <b>Find What</b> text box. Updates the
        ///     <see cref="P:TextReplacementApp.AppConfig.FindWhat" /> property with the
        ///     trimmed text from the directory path text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void OnTextChangedSearchText(object sender, EventArgs e)
        {
            if (_appConfig != null) _appConfig.FindWhat = txtSearchText.Text.Trim();
        }

        private string ReadTextFromMemory(UnmanagedMemoryAccessor? accessor, long length)
        {
            var result = string.Empty;

            try
            {
                // check for conditions that would prohibit our success
                if (accessor == null) return result;
                if (!accessor.CanRead) return result;
                if (length <= 0L) return result;

                var bytes = new byte[length];
                // read bytes from memory
                accessor.ReadArray(0, bytes, 0, (int)length);
                // decode the bytes read from memory into a utf8 string
                result = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this, ex.Message, Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Stop
                );

                result = string.Empty;
            }

            return result;
        }

        /// <summary>
        ///     Replaces text in the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file to perform text replacement on.</param>
        /// <param name="searchText">The text to search for in the file.</param>
        /// <param name="replaceText">The text to replace the search text with.</param>
        /// <remarks>
        ///     This method performs text replacement in the specified file. It reads
        ///     the content of the file, replaces occurrences of the search text with the
        ///     replace text, and writes the modified content back to the file. If the file
        ///     path contains specific directories (such as <c>.git</c>, <c>.vs</c>, etc.), it
        ///     skips the replacement.
        /// </remarks>
        private void ReplaceTextInFile(string filePath, string searchText,
            string replaceText)
        {
            if (filePath.ContainsAnyOf(FileFilterList)) return;

            if (!Path.GetExtension(filePath).IsAnyOf(".txt", ".cs", ".resx", ".config", ".json",
                    ".csproj", ".settings", ".md")) return;

            // File.Open opens a file stream
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite,
                FileShare.None);
            var originalLength = fileStream.Length;

            if (originalLength == 0) return;

            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(
                fileStream, null, originalLength, MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, false
            );

            using var accessor = memoryMappedFile.CreateViewAccessor(0, originalLength,
                MemoryMappedFileAccess.ReadWrite);

            var text = ReadTextFromMemory(accessor, originalLength);
            if (string.IsNullOrWhiteSpace(text)) return;

            text = text.Replace(searchText, replaceText);

            long modifiedLength = Encoding.UTF8.GetByteCount(text);

            if (modifiedLength > originalLength)
            {
                fileStream.SetLength(modifiedLength);
                fileStream.Seek(0, SeekOrigin.Begin);
                using var newMemoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null,
                    modifiedLength, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None,
                    false);
                using var newAccessor = newMemoryMappedFile.CreateViewAccessor(0,
                    modifiedLength, MemoryMappedFileAccess.ReadWrite);
                WriteTextToMemory(newAccessor, text);
            }
            else
            {
                WriteTextToMemory(accessor, text);
            }
        }

        /// <summary>
        ///     Replaces text in all files within the specified directory and its subdirectories
        /// </summary>
        /// <param name="directoryPath">The path of the directory to search for files</param>
        /// <param name="searchText">text to search for</param>
        /// <param name="replaceText">text to replace with</param>
        /// <param name="progressReporter"></param>
        /// ///
        /// <remarks>
        ///     This method recursively searches for files within the specified
        ///     directory and its subdirectories. For each file found, it calls
        ///     <see cref="ReplaceTextInFile" /> to perform text replacement. Certain
        ///     directories (e.g., <c>.git</c>, <c>.vs</c>, etc.) are excluded from text
        ///     replacement.  Modify that part of the code to suit your taste.
        /// </remarks>
        private void ReplaceTextInFiles(string directoryPath, string searchText, string
            replaceText, IProgress<ProgressReport> progressReporter)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    MessageBox.Show(
                        this, $"ERROR: The folder {directoryPath} was not found",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop
                    );
                    return;
                }

                var files = Directory
                    .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Where(file => !file.ContainsAnyOf(FileFilterList)).ToList();

                var totalFiles = files.Count;
                var completedFiles = 0;

                foreach (var file in files.Where(File.Exists))
                {
                    ReplaceTextInFile(file, searchText, replaceText);
                    Interlocked.Increment(ref completedFiles);

                    var progressPercent = (int)((double)completedFiles / totalFiles * 100);
                    var progressReport = new ProgressReport(file, progressPercent);
                    progressReporter.Report(progressReport);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this, ex.Message, Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Stop
                );
            }
        }

        private void WriteTextToMemory(UnmanagedMemoryAccessor? accessor, string text)
        {
            try
            {
                if (accessor is not { CanWrite: true }) return;
                if (string.IsNullOrWhiteSpace(text)) return;

                var bytes = Encoding.UTF8.GetBytes(text);

                accessor.WriteArray(0, bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this, ex.Message, Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Stop
                );
            }
        }

        /// <summary>
        ///     Saves the current application configuration to a JSON file.
        /// </summary>
        /// <remarks>
        ///     The method first checks if the directory containing the configuration
        ///     file exists, and creates it if it does not. Then, it serializes the
        ///     <see cref="T:TextReplacementApp.AppConfig" /> object to JSON format using
        ///     <c>Newtonsoft.Json</c>, and writes the JSON string to the configuration file.
        /// </remarks>
        private void SaveConfig()
        {
            try
            {
                // check for any conditions that might prevent us from succeeding.
                if (string.IsNullOrWhiteSpace(_configFilePath)) return;

                var directory = Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory ?? string.Empty);

                var json = JsonConvert.SerializeObject(
                    _appConfig, Formatting.Indented
                );
                if (string.IsNullOrWhiteSpace(json)) return;

                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                // display an alert with the exception text
                MessageBox.Show(
                    this, ex.Message, Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Stop
                );
            }
        }

        /// <summary>
        ///     Updates the text boxes on the main form with the values stored in the
        ///     application configuration.
        /// </summary>
        /// <remarks>
        ///     This method retrieves the directory path, search text, and replace
        ///     text from the <see cref="T:TextReplacementApp.AppConfig" /> object and sets the
        ///     corresponding text properties of the text boxes on the main form to these
        ///     values.
        /// </remarks>
        private void UpdateTextBoxesFromConfig()
        {
            txtDirectoryPath.Text = _appConfig?.DirectoryPath;
            txtSearchText.Text = _appConfig?.FindWhat;
            txtReplaceText.Text = _appConfig?.ReplaceWith;
        }
    }
}