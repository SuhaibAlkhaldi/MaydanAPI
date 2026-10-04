namespace Maydan.Application.Interfaces;


public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,

        string subfolder,
        CancellationToken cancellationToken = default);
}
