using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartSchool.Schema;

namespace SmartSchool.Tests.Helpers;

/// <summary>
/// Creates an isolated, in-memory AppDbContext per test.
/// Each call with no name argument gets its own database so tests never share state.
/// </summary>
internal static class TestDb
{
    internal static AppDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new AppDbContext(options, httpContextAccessor.Object);
    }
}
