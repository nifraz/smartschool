using System.Text.Json;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using SmartSchool.Graphql.Subscriptions;
using SmartSchool.Resources;
using SmartSchool.Schema;

namespace SmartSchool.Graphql.Mutations;

/// <summary>
/// Resource-key driven CRUD. After each operation publishes a ResourceChangedEvent
/// to the matching topic so subscribed clients refresh immediately.
/// </summary>
[ExtendObjectType<Mutation>]
public class GenericMutation
{
    public async Task<long> CreateResourceAsync(
        string resource,
        JsonElement input,
        [Service] ResourceRegistry registry,
        [Service] ITopicEventSender sender,
        AppDbContext db,
        CancellationToken ct)
    {
        var d = registry.Get(resource) ?? throw new GraphQLException($"Unknown resource '{resource}'");
        var entity = Activator.CreateInstance(d.ClrType)!;
        Apply(entity, input);
        db.Add(entity);
        await db.SaveChangesAsync(ct);
        var id = (long)d.ClrType.GetProperty("Id")!.GetValue(entity)!;
        await sender.SendAsync(resource, new ResourceChangedEvent(resource, "created", id), ct);
        return id;
    }

    public async Task<bool> UpdateResourceAsync(
        string resource, long id, JsonElement input,
        [Service] ResourceRegistry registry,
        [Service] ITopicEventSender sender,
        AppDbContext db,
        CancellationToken ct)
    {
        var d = registry.Get(resource) ?? throw new GraphQLException($"Unknown resource '{resource}'");
        var entity = await db.FindAsync(d.ClrType, id) ?? throw new GraphQLException("Not found");
        Apply(entity, input);
        await db.SaveChangesAsync(ct);
        await sender.SendAsync(resource, new ResourceChangedEvent(resource, "updated", id), ct);
        return true;
    }

    public async Task<bool> DeleteResourceAsync(
        string resource, long id,
        [Service] ResourceRegistry registry,
        [Service] ITopicEventSender sender,
        AppDbContext db,
        CancellationToken ct)
    {
        var d = registry.Get(resource) ?? throw new GraphQLException($"Unknown resource '{resource}'");
        var entity = await db.FindAsync(d.ClrType, id) ?? throw new GraphQLException("Not found");
        db.Remove(entity);
        await db.SaveChangesAsync(ct);
        await sender.SendAsync(resource, new ResourceChangedEvent(resource, "deleted", id), ct);
        return true;
    }

    private static void Apply(object entity, JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object) return;
        var type = entity.GetType();
        foreach (var prop in input.EnumerateObject())
        {
            var pi = type.GetProperty(
                prop.Name,
                System.Reflection.BindingFlags.IgnoreCase |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (pi is null || !pi.CanWrite) continue;
            try
            {
                var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), pi.PropertyType);
                pi.SetValue(entity, value);
            }
            catch { /* type mismatch — skip field */ }
        }
    }
}
