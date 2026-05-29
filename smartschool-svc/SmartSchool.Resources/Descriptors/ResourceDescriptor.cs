namespace SmartSchool.Resources.Descriptors;

public enum FieldType
{
    String, Int, Long, Decimal, Bool, Date, DateTime, Enum, Ref, RefCollection, Json
}

public sealed record ResourceDescriptor(
    string Key,
    string Plural,
    string Module,
    string Icon,
    string Label,
    Type ClrType,
    bool IsSystem,
    int SortOrder,
    IReadOnlyList<FieldDescriptor> Fields,
    IReadOnlyList<RelationDescriptor> Relations,
    IReadOnlyList<ActionDescriptor> Actions);

public sealed record FieldDescriptor(
    string Key,
    string Label,
    FieldType Type,
    bool Required,
    bool Unique,
    bool InGrid,
    bool InForm,
    string? RefResource,
    string? EnumName,
    IReadOnlyList<string> EnumValues,
    string? VisibleWhen,
    int SortOrder);

public sealed record RelationDescriptor(
    string Key,
    string TargetResource,
    string Kind, // one|many
    string ForeignKey,
    string? Label,
    bool ShowInDetail);

public sealed record ActionDescriptor(
    string Key,
    string Label,
    string? Icon,
    string? Permission,
    string Kind);

public sealed record NavNodeDescriptor(
    string Key,
    string Label,
    string? Icon,
    string? ResourceKey,
    string? Path,
    string? Permission,
    int SortOrder,
    IReadOnlyList<NavNodeDescriptor> Children);
