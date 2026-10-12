namespace Maydan.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string subfolder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string filePath,
        Stream stream,
        string fileName,
        string subfolder,
        CancellationToken cancellationToken = default);
}