using Toolbox.Core.Entities;
using Toolbox.Core.Enums;

namespace Toolbox.Core.Interfaces;

public record FileUploadData(Stream Content, string FileName, string ContentType, long Length);

public interface IFileService
{
    Task<TodoAttachment> SaveFileAsync(FileUploadData file, Guid todoItemId);
    Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(string storedPath, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string storedPath);
    bool IsAllowedFile(FileUploadData file);
    FileType DetermineFileType(string contentType);
}
