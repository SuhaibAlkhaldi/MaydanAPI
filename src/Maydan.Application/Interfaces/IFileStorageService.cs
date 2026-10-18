namespace Maydan.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subfolder,
        CancellationToken cancellationToken = default);
    //C:\Users\User\Desktop\maydan\src\Maydan.Application\Interfaces\IFileStorageService.cs
}