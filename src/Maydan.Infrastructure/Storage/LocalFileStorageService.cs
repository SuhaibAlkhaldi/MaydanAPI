using Maydan.Application.Interfaces;

namespace Maydan.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(string rootPath)
    {
        _rootPath = rootPath;
    }

  
    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subfolder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        //genertae new path to prevent redundancy and tracking paths
        var safeFileName = $"{Guid.NewGuid():N}{extension}";

        var relativeSubfolder = subfolder.Replace('\\', '/').Trim('/');

        var targetDirectory = Path.Combine(_rootPath, relativeSubfolder);


        Directory.CreateDirectory(targetDirectory);
        // in the path we selected write and save content file
        var targetPath = Path.Combine(targetDirectory, safeFileName);
        await using var output = File.Create(targetPath);
        await content.CopyToAsync(output, cancellationToken);
        // return the path 
        return $"/uploads/{relativeSubfolder}/{safeFileName}";
    }

  
    public Task DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        var relativePath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.Combine(_rootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
