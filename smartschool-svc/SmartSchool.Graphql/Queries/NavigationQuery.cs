using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using SmartSchool.Schema;
using SmartSchool.Schema.Entities;

namespace SmartSchool.Graphql.Queries;

[ExtendObjectType<Query>]
public class NavigationQuery
{
    /// <summary>
    /// Tree of navigation nodes. For now it is derived from resources;
    /// once the `nav_node` table is populated, this resolver should read from it.
    /// </summary>
    public IEnumerable<NavNodeDto> GetNavigation([Service] Resources.ResourceRegistry registry)
    {
        return registry.All
            .GroupBy(r => r.Module)
            .Select(g => new NavNodeDto(
                Key: g.Key,
                Label: g.Key,
                Icon: "mat:folder",
                ResourceKey: null,
                Path: null,
                PermissionCode: null,
                SortOrder: 0,
                Children: g.OrderBy(x => x.SortOrder).Select(r => new NavNodeDto(
                    Key: r.Key,
                    Label: r.Label,
                    Icon: r.Icon,
                    ResourceKey: r.Key,
                    Path: null,
                    PermissionCode: $"{r.Key}:read",
                    SortOrder: r.SortOrder,
                    Children: Array.Empty<NavNodeDto>())).ToList()));
    }
}

[ExtendObjectType<Query>]
public class TranslationsQuery
{
    [UseProjection]
    public IQueryable<Translation> GetTranslations(
        AppDbContext db,
        string locale,
        string? @namespace = null) =>
        db.Translations
            .Where(t => t.LocaleCode == locale)
            .Where(t => @namespace == null || t.Namespace == @namespace);

    public IQueryable<Locale> GetLocales(AppDbContext db) =>
        db.Locales.Where(l => l.IsEnabled).OrderBy(l => l.SortOrder);
}

public sealed record NavNodeDto(
    string Key, string Label, string? Icon,
    string? ResourceKey, string? Path, string? PermissionCode,
    int SortOrder, IReadOnlyList<NavNodeDto> Children);
