using System.IO.MemoryMappedFiles;
using System.Text;

namespace search_replace
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Compares the <paramref name="value" /> object with the
        ///     <paramref name="testObjects" /> provided, to see if any of the
        ///     <paramref name="testObjects" /> is a match.
        /// </summary>
        /// <param name="value">Source object to check</param>
        /// <param name="testObjects">
        ///     Object or objects that should be compared to value with the
        ///     <see cref="M:System.Objects.Equals" /> method
        /// </param>
        /// <typeparam name="T">Type of the object to be tested</typeparam>
        /// <returns>
        ///     True if any of the <paramref name="testObjects" /> equals the value;
        ///     false otherwise.
        /// </returns>
        public static bool IsAnyOf<T>(this T value, params T[] testObjects)
        {
            return testObjects.Contains
                (value);
        }


        public static bool ContainsAnyOf(this string value, params string[] filterList)
        {
            return filterList.Any(value.Contains);
        }
    }

    internal static class Program
    {
        private static readonly string[] FileFilterList = { @"\.git\", @"\.vs\", @"\packages\", @"\bin\", @"\obj\" };
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Console.Title = "Text Replacement Console App";
            const string directoryPath = @"D:\Projects\App-Workspace\hooks-ts";
            const string searchText = "equivalent of";
            const string replaceText = "same as";

            Console.WriteLine($"Searching all code in '{directoryPath}'...");
            Console.WriteLine($"Replacing '{searchText}' with '{replaceText}'...");

            Console.WriteLine($"Start Time: {DateTime.Now:O}");

            ReplaceTextInFiles(directoryPath, searchText, replaceText);

            Console.WriteLine($"End Time: {DateTime.Now:O}");
            Console.WriteLine("Text replacement completed.");

            Console.ReadKey();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // ApplicationConfiguration.Initialize();
            // Application.Run(new Form1());
        }

        /// <summary>
        ///     Reads text from a memory-mapped file accessor.
        /// </summary>
        /// <param name="accessor">The memory-mapped file accessor.</param>
        /// <param name="length">The length of the text to read.</param>
        /// <returns>The text read from memory.</returns>
        /// <remarks>
        ///     This method reads text from a memory-mapped file accessor and returns it as a
        ///     string.
        ///     It reads the specified length of bytes from the accessor and decodes them using
        ///     UTF-8 encoding.
        /// </remarks>
        private static string ReadTextFromMemory(UnmanagedMemoryAccessor? accessor, long length)
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
                Console.Write($"ERROR: {ex.Message}");
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
        private static void ReplaceTextInFile(string filePath, string searchText,
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
        /// Replaces text in all files within the specified directory and its subdirectories
        /// </summary>
        /// <param name="directoryPath">The path of the directory to search for files</param>
        /// <param name="searchText">text to search for</param>
        /// <param name="replaceText">text to replace with</param>
        ///  /// <remarks>
        /// This method recursively searches for files within the specified
        /// directory and its subdirectories. For each file found, it calls
        /// <see cref="ReplaceTextInFile" /> to perform text replacement. Certain
        /// directories (e.g., <c>.git</c>, <c>.vs</c>, etc.) are excluded from text
        /// replacement.  Modify that part of the code to suit your taste.
        /// </remarks>
        private static void ReplaceTextInFiles(string directoryPath, string searchText, string replaceText)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"ERROR: The folder {directoryPath} was not found");
                    return;
                }

                var files = Directory
                    .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Where(file => !file.ContainsAnyOf(FileFilterList)).ToList();

                var completedFiles = 0;

                foreach (var file in files.Where(File.Exists))
                {
                    ReplaceTextInFile(file, searchText, replaceText);
                    Interlocked.Increment(ref completedFiles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        private static void WriteTextToMemory(UnmanagedMemoryAccessor? accessor, string text)
        {
            try
            {
                if (accessor == null) return;
                if (!accessor.CanWrite) return;
                if (string.IsNullOrWhiteSpace(text)) return;

                var bytes = Encoding.UTF8.GetBytes(text);
                
                accessor.WriteArray(0, bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
    
    
}