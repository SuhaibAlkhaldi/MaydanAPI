using Maydan.Application.Interfaces;

namespace Maydan.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string subfolder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeSubfolder = subfolder.Replace('\\', '/').Trim('/');
        var targetDirectory = Path.Combine(_rootPath, relativeSubfolder);

        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, safeFileName);
        await using var output = File.Create(targetPath);
        await stream.CopyToAsync(output, cancellationToken);

        return $"/uploads/{relativeSubfolder}/{safeFileName}";

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsRootPath;

    public LocalFileStorageService(string uploadsRootPath)
    {
        _uploadsRootPath = uploadsRootPath;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subfolder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var folderPath = Path.Combine(_uploadsRootPath, subfolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        // Web-relative, forward-slash path regardless of host OS — matches how
        // app.UseStaticFiles() serves wwwroot content and how the frontend would request it back.
        return $"/uploads/{subfolder}/{fileName}".Replace('\\', '/');
    }
}
