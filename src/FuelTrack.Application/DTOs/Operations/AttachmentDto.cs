namespace FuelTrack.Application.DTOs.Operations;

public sealed record AttachmentDto(
    int Id,
    string OriginalFileName,
    string ContentType,
    long ByteSize,
    string Sha256,
    DateTime CreatedAt);

public sealed record UploadAttachmentDto(byte[] Content, string ContentType, string OriginalFileName);
