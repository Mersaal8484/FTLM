using Microsoft.AspNetCore.Http;

namespace FuelTrack.Tests.Services;

public class StubHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
