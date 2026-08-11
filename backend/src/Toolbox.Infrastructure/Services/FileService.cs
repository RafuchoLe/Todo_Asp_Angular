using Microsoft.Extensions.Configuration;
using Toolbox.Core.Entities;
using Toolbox.Core.Enums;
using Toolbox.Core.Interfaces;

namespace Toolbox.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IConfiguration _configuration;

    public FileService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<TodoAttachment> SaveFileAsync(FileUploadData file, Guid todoItemId)
    {
        var uploadPath = _configuration["FileUpload:UploadPath"] ?? "Uploads";
        var directory = Path.Combine(uploadPath, "todo-attachments");

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var storedPath = Path.Combine(directory, storedFileName);

        await using var stream = new FileStream(storedPath, FileMode.Create);
        await file.Content.CopyToAsync(stream);

        return new TodoAttachment
        {
            FileName = file.FileName,
            StoredPath = storedPath,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            FileType = DetermineFileType(file.ContentType),
            TodoItemId = todoItemId
        };
    }

    public Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(
        string storedPath, string fileName, string contentType)
    {
        if (!File.Exists(storedPath))
            return Task.FromResult<(Stream, string, string)?>(null);

        Stream stream = new FileStream(storedPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<(Stream, string, string)?>((stream, contentType, fileName));
    }

    public Task<bool> DeleteFileAsync(string storedPath)
    {
        if (!File.Exists(storedPath))
            return Task.FromResult(false);

        File.Delete(storedPath);
        return Task.FromResult(true);
    }

    public bool IsAllowedFile(FileUploadData file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();

        if (contentType.StartsWith("image/"))
        {
            var allowed = GetAllowedExtensions("FileUpload:AllowedImageExtensions");
            var maxMb = long.TryParse(_configuration["FileUpload:MaxFileSizeMB"], out var v) ? v : 10L;
            return allowed.Contains(extension) && file.Length <= maxMb * 1024 * 1024;
        }

        if (contentType.StartsWith("video/"))
        {
            var allowed = GetAllowedExtensions("FileUpload:AllowedVideoExtensions");
            var maxMb = long.TryParse(_configuration["FileUpload:MaxVideoSizeMB"], out var v) ? v : 100L;
            return allowed.Contains(extension) && file.Length <= maxMb * 1024 * 1024;
        }

        if (contentType.StartsWith("audio/"))
        {
            var allowed = GetAllowedExtensions("FileUpload:AllowedAudioExtensions");
            var maxMb = long.TryParse(_configuration["FileUpload:MaxAudioSizeMB"], out var v) ? v : 50L;
            return allowed.Contains(extension) && file.Length <= maxMb * 1024 * 1024;
        }

        return false;
    }

    private string[] GetAllowedExtensions(string key)
    {
        return _configuration.GetSection(key).GetChildren()
            .Select(c => c.Value ?? string.Empty)
            .Where(v => v.Length > 0)
            .ToArray();
    }

    public FileType DetermineFileType(string contentType)
    {
        if (contentType.StartsWith("image/")) return FileType.Image;
        if (contentType.StartsWith("video/")) return FileType.Video;
        if (contentType.StartsWith("audio/")) return FileType.Audio;
        return FileType.Image;
    }
}
