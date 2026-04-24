using System.IO;

namespace WasteCollection_RecyclingPlatform.Services.Helpers;

public static class FileHelper
{
    public static void DeleteFileIfExists(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;

        try
        {
            // Standardize relative URL to physical path
            // relativeUrl is like /report-images/1/file.jpg
            var path = relativeUrl.TrimStart('/');
            var fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", path);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch
        {
            // Silently fail to not break business logic if file is locked or missing
        }
    }
}
