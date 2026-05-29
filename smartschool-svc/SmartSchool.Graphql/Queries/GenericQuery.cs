using System.Text.Json;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using SmartSchool.Resources;
using SmartSchool.Schema;

namespace SmartSchool.Graphql.Queries;

/// <summary>
/// Resource-key driven reads: one query surface for every entity.
/// Returns a paged list of camelCase JSON strings; the frontend resource-engine
/// parses them by matching field descriptor keys.
/// </summary>
[ExtendObjectType<Query>]
public class GenericQuery
{
    /// <summary>Returns a page of records for the given resource key.</summary>
    public async Task<GenericPage> ResourceItemsAsync(
        string resource,
        int skip,
        int take,
        [Service] ResourceRegistry registry,
        AppDbContext db,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 200);
        var d = registry.Get(resource)
            ?? throw new GraphQLException($"Unknown resource '{resource}'");

        var helper = typeof(GenericQuery)
            .GetMethod(nameof(QueryPageAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(d.ClrType);

        return await (Task<GenericPage>)helper.Invoke(null, [db, skip, take, ct])!;
    }

    /// <summary>Returns a single record by resource key + id, or null.</summary>
    public async Task<string?> ResourceItemAsync(
        string resource,
        long id,
        [Service] ResourceRegistry registry,
        AppDbContext db,
        CancellationToken ct)
    {
        var d = registry.Get(resource)
            ?? throw new GraphQLException($"Unknown resource '{resource}'");

        var helper = typeof(GenericQuery)
            .GetMethod(nameof(FindItemAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(d.ClrType);

        return await (Task<string?>)helper.Invoke(null, [db, id, ct])!;
    }

    // ── Typed helpers (called via MakeGenericMethod) ─────────────────────────

    private static async Task<GenericPage> QueryPageAsync<T>(
        AppDbContext db, int skip, int take, CancellationToken ct) where T : class
    {
        var q = db.Set<T>();
        var total = await q.CountAsync(ct);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return new GenericPage(total, items.Select(Serialize).ToList());
    }

    private static async Task<string?> FindItemAsync<T>(
        AppDbContext db, long id, CancellationToken ct) where T : class
    {
        var entity = await db.Set<T>().FindAsync([id], ct);
        return entity is null ? null : Serialize(entity);
    }

    private static readonly JsonSerializerOptions SerializerOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    };

    private static string Serialize(object entity)
        => JsonSerializer.Serialize(entity, SerializerOpts);
}

public sealed record GenericPage(int Total, IReadOnlyList<string> Items);
