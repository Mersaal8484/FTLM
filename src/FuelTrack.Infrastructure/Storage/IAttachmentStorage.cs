namespace FuelTrack.Infrastructure.Storage;

public interface IAttachmentStorage
{
    Task WriteAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
