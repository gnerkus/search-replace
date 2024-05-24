namespace search_replace
{
    /// <summary>
    /// Represents a progress report containing information about the current file being 
    /// processed and the progress percentage
    /// </summary>
    public class ProgressReport
    {
        public ProgressReport(string currentFile, int progressPercent)
        {
            CurrentFile = currentFile;
            ProgressPercent = progressPercent;
        }
        
        public string CurrentFile { get; }
        public int ProgressPercent { get; }
    }
}