using System.Text.Json;

namespace AICodeReviewer.Web;

public static class DocumentService
{
    private static readonly string[] SupportedExtensions = { ".md" };
    
    public static (List<string> files, bool isError) ScanDocumentsFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return (new List<string>(), false); // Empty list, not error
            
            var markdownFiles = Directory.GetFiles(folderPath, "*.md")
                                       .Select(f => Path.GetFileNameWithoutExtension(f)!)
                                       .OrderBy(name => name)
                                       .ToList();
                                       
            return (markdownFiles, false);
        }
        catch (UnauthorizedAccessException)
        {
            return (new List<string>(), true); // Return empty list with error flag
        }
        catch (Exception)
        {
            return (new List<string>(), true); // Return empty list with error flag
        }
    }
    
    public static (string content, bool isError) LoadDocument(string fileName, string folderPath)
    {
        var fullPath = Path.Combine(folderPath, fileName + ".md");
        
        try
        {
            if (!File.Exists(fullPath))
                return ($"Document not found: {fileName}", true);
                
            var content = File.ReadAllText(fullPath);
            return (content, false);
        }
        catch (Exception ex)
        {
            return ($"Error reading document: {ex.Message}", true);
        }
    }
    
    public static async Task<(string content, bool isError)> LoadDocumentAsync(string fileName, string folderPath)
    {
        var fullPath = Path.Combine(folderPath, fileName + ".md");
        
        try
        {
            if (!File.Exists(fullPath))
                return ($"Document not found: {fileName}", true);
                
            var content = await File.ReadAllTextAsync(fullPath);
            return (content, false);
        }
        catch (Exception ex)
        {
            return ($"Error reading document: {ex.Message}", true);
        }
    }
    
    public static string GetDocumentDisplayName(string fileName)
    {
        // Convert filename to friendly display name
        return fileName.Replace("-", " ").Replace("_", " ");
    }
}

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }
    
    public static T? GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}