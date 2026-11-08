namespace FuelTrack.Domain.Models;

/// <summary>
/// Associates an attachment with an aggregate without coupling the storage model
/// to every future aggregate type.
/// </summary>
public sealed class AttachmentLink : BaseEntity
{
    public int AttachmentId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int SortOrder { get; set; }

    public Attachment Attachment { get; set; } = null!;
}
