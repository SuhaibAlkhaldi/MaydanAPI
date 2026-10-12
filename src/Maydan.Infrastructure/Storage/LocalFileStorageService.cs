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

        // Web-relative, forward-slash path regardless of host OS — matches how
        // app.UseStaticFiles() serves wwwroot content and how the frontend would request it back.
        return $"/uploads/{subfolder}/{fileName}".Replace('\\', '/');
    }
public Task<string> SaveAsync(Stream content, string originalFileName)
    {
        throw new NotImplementedException();
    }

   
}