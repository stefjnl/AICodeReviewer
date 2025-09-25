namespace AICodeReviewer.Web.Models;

/// <summary>
/// Represents a file system item (file or directory)
/// </summary>
public class FileSystemItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
}