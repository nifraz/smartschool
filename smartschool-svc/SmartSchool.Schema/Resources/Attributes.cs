namespace SmartSchool.Schema.Resources;

/// <summary>
/// Marks an entity as a Resource exposed through the dynamic UI engine.
/// Defined here (not in SmartSchool.Resources) to avoid circular project references.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ResourceAttribute : Attribute
{
    public string Key { get; }
    public string? Plural { get; init; }
    public string? Module { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public bool IsSystem { get; init; }
    public int SortOrder { get; init; }

    public ResourceAttribute(string key) => Key = key;
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class FieldAttribute : Attribute
{
    public string? Label { get; init; }
    public bool Required { get; init; }
    public bool Unique { get; init; }
    public bool InGrid { get; init; } = true;
    public bool InForm { get; init; } = true;
    public string? RefResource { get; init; }
    public string? VisibleWhen { get; init; }
    public int SortOrder { get; init; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class HiddenFieldAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ResourceActionAttribute : Attribute
{
    public string Key { get; }
    public string? Label { get; init; }
    public string? Icon { get; init; }
    public string? Permission { get; init; }
    public string Kind { get; init; } = "row"; // row|bulk|global

    public ResourceActionAttribute(string key) => Key = key;
}
