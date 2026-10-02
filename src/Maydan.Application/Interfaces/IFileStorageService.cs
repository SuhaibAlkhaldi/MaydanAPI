namespace Maydan.Application.Interfaces;


public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,

public interface IFileStorageService
{

    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subfolder,
        CancellationToken cancellationToken = default);
}
