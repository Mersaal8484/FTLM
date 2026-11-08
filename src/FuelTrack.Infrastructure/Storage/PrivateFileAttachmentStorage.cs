using Microsoft.Extensions.Options;

namespace FuelTrack.Infrastructure.Storage;

/// <summary>Local development/default storage. It is deliberately outside wwwroot.</summary>
public sealed class PrivateFileAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;

    public PrivateFileAttachmentStorage(IOptions<AttachmentStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "attachments"));
    }

    public async Task WriteAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    private string Resolve(string storageKey)
    {
        var normalised = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_rootPath, normalised));
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Attachment storage key is invalid.");
        return path;
    }
}
