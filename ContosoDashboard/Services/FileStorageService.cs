namespace ContosoDashboard.Services;

/// <summary>
/// Storage abstraction for document files. The training build stores files on the local
/// filesystem; production can swap in an Azure Blob Storage implementation via DI.
/// Keys are relative paths such as "{userId}/{projectId|personal}/{guid}.pdf".
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string filePath);
    Task<Stream> DownloadAsync(string filePath);
    Task<string> GetUrlAsync(string filePath, TimeSpan expiration);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["FileStorage:RootPath"] ?? "AppData/uploads";

        // Relative roots resolve against the content root (outside wwwroot)
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot));

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        // fileName is the server-generated storage key, never the user-supplied name
        var fullPath = GetSafeFullPath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(output);

        return fileName;
    }

    public Task DeleteAsync(string filePath)
    {
        var fullPath = GetSafeFullPath(filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string filePath)
    {
        var fullPath = GetSafeFullPath(filePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration)
    {
        // Local files are served only through authorized endpoints, so there is no direct URL.
        // An Azure implementation would return a time-limited SAS URL here.
        return Task.FromResult(string.Empty);
    }

    private string GetSafeFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Invalid storage path.", nameof(relativePath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

        // Path traversal protection: the resolved path must stay inside the storage root
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid storage path.", nameof(relativePath));
        }

        return fullPath;
    }
}
