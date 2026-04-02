namespace ClaudeCode.Constants;

public static class FileConstants
{
    public static readonly IReadOnlySet<string> BinaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".tiff", ".tif",
        // Videos
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v", ".mpeg", ".mpg",
        // Audio
        ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a", ".wma", ".aiff", ".opus",
        // Archives
        ".zip", ".tar", ".gz", ".bz2", ".7z", ".rar", ".xz", ".z", ".tgz", ".iso",
        // Executables/binaries
        ".exe", ".dll", ".so", ".dylib", ".bin", ".o", ".a", ".obj", ".lib", ".app", ".msi", ".deb", ".rpm",
        // Documents
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp",
        // Fonts
        ".ttf", ".otf", ".woff", ".woff2", ".eot",
        // Bytecode / VM artifacts
        ".pyc", ".pyo", ".class", ".jar", ".war", ".ear", ".node", ".wasm", ".rlib",
        // Database files
        ".sqlite", ".sqlite3", ".db", ".mdb", ".idx",
        // Design / 3D
        ".psd", ".ai", ".eps", ".sketch", ".fig", ".xd", ".blend", ".3ds", ".max",
        // Flash
        ".swf", ".fla",
        // Lock/profiling data
        ".lockb", ".dat", ".data",
    };

    public static bool HasBinaryExtension(string filePath)
    {
        var lastDot = filePath.LastIndexOf('.');
        if (lastDot < 0) return false;
        return BinaryExtensions.Contains(filePath[lastDot..]);
    }

    private const int BinaryCheckSize = 8192;

    public static bool IsBinaryContent(byte[] buffer)
    {
        var checkSize = Math.Min(buffer.Length, BinaryCheckSize);
        var nonPrintable = 0;

        for (var i = 0; i < checkSize; i++)
        {
            var b = buffer[i];
            if (b == 0) return true;
            if (b < 32 && b != 9 && b != 10 && b != 13)
                nonPrintable++;
        }

        return (double)nonPrintable / checkSize > 0.1;
    }
}
