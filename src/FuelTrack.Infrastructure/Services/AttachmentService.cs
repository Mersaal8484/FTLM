using System.Security.Cryptography;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class AttachmentService(
    FuelTrackDbContext db,
    IAttachmentStorage storage,
    ITenantRequestContext? tenant = null) : IAttachmentService
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };

    public async Task<Result<AttachmentDto>> StoreAsync(UploadAttachmentDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (dto.Content.Length == 0 || dto.Content.Length > StationOperationalSettings.DefaultReadingPhotoMaxBytes)
            return Result<AttachmentDto>.Failure("Image must be greater than zero and no larger than 65 KB.");
        if (!AllowedTypes.TryGetValue(dto.ContentType, out var extension) || !MatchesSignature(dto.Content, dto.ContentType))
            return Result<AttachmentDto>.Failure("Only valid JPEG or WebP images are accepted.");

        var safeName = Path.GetFileName(dto.OriginalFileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = $"attachment{extension}";
        var key = BuildStorageKey(extension);
        var sha256 = Convert.ToHexString(SHA256.HashData(dto.Content)).ToLowerInvariant();

        await storage.WriteAsync(key, dto.Content, cancellationToken);
        var attachment = new Attachment
        {
            StorageKey = key,
            OriginalFileName = safeName,
            ContentType = dto.ContentType.ToLowerInvariant(),
            ByteSize = dto.Content.Length,
            Sha256 = sha256,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AttachmentDto>.Success(ToDto(attachment), 201);
    }

    public async Task<(AttachmentDto Metadata, Stream Content)?> OpenReadAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);
        if (attachment is null) return null;
        var stream = await storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return stream is null ? null : (ToDto(attachment), stream);
    }

    public async Task<Result> LinkAsync(int attachmentId, string entityType, int entityId, int sortOrder, int userId, CancellationToken cancellationToken = default)
    {
        if (entityId <= 0 || sortOrder < 0 || string.IsNullOrWhiteSpace(entityType))
            return Result.Failure("Attachment link is invalid.");
        if (!await db.Attachments.AnyAsync(x => x.Id == attachmentId, cancellationToken))
            return Result.NotFound("Attachment was not found.");

        var duplicate = await db.AttachmentLinks.AnyAsync(x => x.AttachmentId == attachmentId && x.EntityType == entityType && x.EntityId == entityId, cancellationToken);
        if (duplicate) return Result.Success();

        db.AttachmentLinks.Add(new AttachmentLink
        {
            AttachmentId = attachmentId,
            EntityType = entityType.Trim(),
            EntityId = entityId,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
        });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(201);
    }

    private string BuildStorageKey(string extension)
    {
        var customer = tenant?.TenantId?.ToString("N") ?? "single";
        return $"customers/{customer}/attachments/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
    }

    private static bool MatchesSignature(byte[] bytes, string contentType) => contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
        ? bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF
        : bytes.Length >= 12 && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

    private static AttachmentDto ToDto(Attachment attachment) => new(
        attachment.Id,
        attachment.OriginalFileName,
        attachment.ContentType,
        attachment.ByteSize,
        attachment.Sha256,
        attachment.CreatedAt);
}
