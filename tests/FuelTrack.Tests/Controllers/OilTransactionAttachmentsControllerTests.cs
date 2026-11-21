using System.Security.Claims;
using FuelTrack.API.Controllers;
using FuelTrack.Application.Constants;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Controllers;

public sealed class OilTransactionAttachmentsControllerTests
{
    [Fact]
    public async Task GetAttachment_DeniesAttachmentOutsideTheCallersScope()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"OilAttachmentRead_{Guid.NewGuid()}").Options);
        db.AddRange(
            new SubStation { Id = 1, Name = "Scoped site", Code = "S1", IsActive = true },
            new OilTransaction { Id = 10, SubStationId = 1, QuantityLiters = 10 },
            new Attachment { Id = 100, StorageKey = "oil-100", OriginalFileName = "oil.jpg", ContentType = "image/jpeg", ByteSize = 100, Sha256 = "hash" },
            new AttachmentLink { AttachmentId = 100, EntityType = AttachmentEntityTypes.OilTransaction, EntityId = 10, SortOrder = 0 });
        await db.SaveChangesAsync();

        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(x => x.GetAccessibleSubStationIdsAsync(7)).ReturnsAsync([2]);
        var controller = new OilTransactionsController(
            new Mock<IOilTransactionService>().Object,
            new Mock<IRealtimeNotifier>().Object,
            new Mock<IAttachmentService>().Object,
            scope.Object,
            new CommercialStationModeService(db),
            db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"))
                }
            }
        };

        var result = await controller.GetAttachment(10, 100, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }
}
