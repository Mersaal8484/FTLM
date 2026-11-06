namespace FuelTrack.Infrastructure.Storage;

public sealed class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";
    public string? RootPath { get; init; }
}
