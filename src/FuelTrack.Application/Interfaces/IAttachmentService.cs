using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IAttachmentService
{
    Task<Result<AttachmentDto>> StoreAsync(UploadAttachmentDto dto, int userId, CancellationToken cancellationToken = default);
    Task<(AttachmentDto Metadata, Stream Content)?> OpenReadAsync(int attachmentId, CancellationToken cancellationToken = default);
    Task<Result> LinkAsync(int attachmentId, string entityType, int entityId, int sortOrder, int userId, CancellationToken cancellationToken = default);
}
