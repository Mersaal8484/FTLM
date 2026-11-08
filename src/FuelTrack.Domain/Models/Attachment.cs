namespace FuelTrack.Domain.Models;

/// <summary>
/// Immutable metadata for a binary object held by the configured private storage provider.
/// </summary>
public sealed class Attachment : BaseEntity
{
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public ICollection<AttachmentLink> Links { get; set; } = new List<AttachmentLink>();
}
