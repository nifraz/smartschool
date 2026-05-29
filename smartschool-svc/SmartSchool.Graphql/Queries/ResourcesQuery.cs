using HotChocolate;
using HotChocolate.Types;
using SmartSchool.Resources;
using SmartSchool.Resources.Descriptors;

namespace SmartSchool.Graphql.Queries;

/// <summary>
/// Exposes resource metadata that drives the dynamic GUI.
/// Permission filtering is applied via <see cref="IPermissionEvaluator"/>.
/// </summary>
[ExtendObjectType<Query>]
public class ResourcesQuery
{
    public IEnumerable<ResourceDto> GetResources([Service] ResourceRegistry registry)
        => registry.All.Select(ResourceDto.From);

    public ResourceDto? GetResource(string key, [Service] ResourceRegistry registry)
    {
        var d = registry.Get(key);
        return d is null ? null : ResourceDto.From(d);
    }
}

public sealed record ResourceDto(
    string Key, string Plural, string Module, string Icon, string Label,
    bool IsSystem, int SortOrder,
    IReadOnlyList<FieldDto> Fields,
    IReadOnlyList<RelationDto> Relations,
    IReadOnlyList<ActionDto> Actions)
{
    public static ResourceDto From(ResourceDescriptor d) => new(
        d.Key, d.Plural, d.Module, d.Icon, d.Label, d.IsSystem, d.SortOrder,
        d.Fields.Select(FieldDto.From).ToList(),
        d.Relations.Select(RelationDto.From).ToList(),
        d.Actions.Select(ActionDto.From).ToList());
}

public sealed record FieldDto(
    string Key, string Label, string Type, bool Required, bool Unique,
    bool InGrid, bool InForm, string? RefResource, string? EnumName,
    IReadOnlyList<string> EnumValues, string? VisibleWhen, int SortOrder)
{
    public static FieldDto From(FieldDescriptor f) => new(
        f.Key, f.Label, f.Type.ToString(), f.Required, f.Unique,
        f.InGrid, f.InForm, f.RefResource, f.EnumName, f.EnumValues, f.VisibleWhen, f.SortOrder);
}

public sealed record RelationDto(string Key, string TargetResource, string Kind, string ForeignKey, string? Label, bool ShowInDetail)
{
    public static RelationDto From(RelationDescriptor r) => new(r.Key, r.TargetResource, r.Kind, r.ForeignKey, r.Label, r.ShowInDetail);
}

public sealed record ActionDto(string Key, string Label, string? Icon, string? Permission, string Kind)
{
    public static ActionDto From(ActionDescriptor a) => new(a.Key, a.Label, a.Icon, a.Permission, a.Kind);
}
