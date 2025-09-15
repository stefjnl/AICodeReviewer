namespace AICodeReviewer.Web.Models;

public class DirectoryBrowseRequest
{
    public string? CurrentPath { get; set; }
    public string? SelectedPath { get; set; }
}

public class DirectoryBrowseResponse
{
    public List<DirectoryItem> Directories { get; set; } = new();
    public List<DirectoryItem> Files { get; set; } = new();
    public string CurrentPath { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
    public bool IsGitRepository { get; set; }
    public string? Error { get; set; }
}

public class DirectoryItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public bool IsGitRepository { get; set; }
    public DateTime LastModified { get; set; }
    public long Size { get; set; }
}