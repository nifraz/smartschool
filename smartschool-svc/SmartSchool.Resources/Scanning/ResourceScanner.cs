using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SmartSchool.Resources.Descriptors;
using SmartSchool.Schema.Resources;

namespace SmartSchool.Resources.Scanning;

/// <summary>
/// Scans an assembly for [Resource]-annotated entities and builds descriptors
/// using attributes + conventions. Pure, deterministic, cache-friendly.
/// </summary>
public sealed class ResourceScanner
{
    public IReadOnlyList<ResourceDescriptor> Scan(params Assembly[] assemblies)
    {
        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<ResourceAttribute>() is not null);

        return types.Select(Build).OrderBy(r => r.SortOrder).ThenBy(r => r.Key).ToList();
    }

    private static ResourceDescriptor Build(Type t)
    {
        var attr = t.GetCustomAttribute<ResourceAttribute>()!;
        var key = attr.Key;

        var fields = new List<FieldDescriptor>();
        var relations = new List<RelationDescriptor>();

        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetCustomAttribute<HiddenFieldAttribute>() is not null) continue;
            if (p.GetCustomAttribute<NotMappedAttribute>() is not null) continue;
            if (IsAuditField(p.Name)) continue;

            var fAttr = p.GetCustomAttribute<FieldAttribute>();
            var (ftype, refRes, enumName, enumValues) = MapType(p);

            if (ftype is FieldType.RefCollection)
            {
                relations.Add(new RelationDescriptor(
                    Key: p.Name,
                    TargetResource: refRes ?? "unknown",
                    Kind: "many",
                    ForeignKey: $"{t.Name.ToLowerInvariant()}_id",
                    Label: fAttr?.Label,
                    ShowInDetail: fAttr?.InGrid ?? true));
                continue;
            }

            if (ftype is FieldType.Ref)
            {
                relations.Add(new RelationDescriptor(
                    Key: p.Name,
                    TargetResource: refRes ?? "unknown",
                    Kind: "one",
                    ForeignKey: $"{p.Name.ToLowerInvariant()}_id",
                    Label: fAttr?.Label,
                    ShowInDetail: fAttr?.InForm ?? true));
                continue;
            }

            var required = fAttr?.Required ?? p.GetCustomAttribute<RequiredAttribute>() is not null
                || (!IsNullable(p) && ftype != FieldType.Bool);

            fields.Add(new FieldDescriptor(
                Key: ToCamel(p.Name),
                Label: fAttr?.Label ?? Humanize(p.Name),
                Type: ftype,
                Required: required,
                Unique: fAttr?.Unique ?? false,
                InGrid: fAttr?.InGrid ?? true,
                InForm: fAttr?.InForm ?? true,
                RefResource: refRes ?? fAttr?.RefResource,
                EnumName: enumName,
                EnumValues: enumValues,
                VisibleWhen: fAttr?.VisibleWhen,
                SortOrder: fAttr?.SortOrder ?? 0));
        }

        var actions = t.GetCustomAttributes<ResourceActionAttribute>()
            .Select(a => new ActionDescriptor(a.Key, a.Label ?? a.Key, a.Icon, a.Permission, a.Kind))
            .ToList();

        return new ResourceDescriptor(
            Key: key,
            Plural: attr.Plural ?? key + "s",
            Module: attr.Module ?? "general",
            Icon: attr.Icon ?? "mat:folder",
            Label: attr.Label ?? Humanize(t.Name),
            ClrType: t,
            IsSystem: attr.IsSystem,
            SortOrder: attr.SortOrder,
            Fields: fields,
            Relations: relations,
            Actions: actions);
    }

    private static (FieldType type, string? refRes, string? enumName, IReadOnlyList<string> enumValues) MapType(PropertyInfo p)
    {
        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        if (t.IsEnum)
            return (FieldType.Enum, null, t.Name, Enum.GetNames(t));

        if (t == typeof(string)) return (FieldType.String, null, null, []);
        if (t == typeof(bool)) return (FieldType.Bool, null, null, []);
        if (t == typeof(int) || t == typeof(short)) return (FieldType.Int, null, null, []);
        if (t == typeof(long)) return (FieldType.Long, null, null, []);
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return (FieldType.Decimal, null, null, []);
        if (t == typeof(DateOnly) || t == typeof(DateTime) && p.Name.EndsWith("Date")) return (FieldType.Date, null, null, []);
        if (t == typeof(DateTime)) return (FieldType.DateTime, null, null, []);

        // Navigation collection?
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
        {
            var inner = t.GetGenericArguments().FirstOrDefault();
            return (FieldType.RefCollection, inner?.Name.ToLowerInvariant(), null, []);
        }

        // Navigation single ref
        if (t.IsClass && t.Assembly == p.DeclaringType?.Assembly)
            return (FieldType.Ref, t.Name.ToLowerInvariant(), null, []);

        return (FieldType.Json, null, null, []);
    }

    private static bool IsNullable(PropertyInfo p) =>
        Nullable.GetUnderlyingType(p.PropertyType) is not null
        || new NullabilityInfoContext().Create(p).WriteState == NullabilityState.Nullable;

    private static bool IsAuditField(string name) => name is
        "Id" or "Notes" or "CreatedTime" or "LastModifiedTime" or "DeletedTime"
        or "CreatedUserId" or "LastModifiedUserId" or "DeletedUserId"
        or "CreatedUser" or "LastModifiedUser" or "DeletedUser";

    private static string ToCamel(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static string Humanize(string s)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1])) sb.Append(' ');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
