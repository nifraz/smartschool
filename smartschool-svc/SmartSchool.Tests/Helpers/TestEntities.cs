using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartSchool.Schema.Resources;

namespace SmartSchool.Tests.Helpers;

// ── Enum used by WidgetEntity ────────────────────────────────────────────────
public enum WidgetColor { Red, Green, Blue }

// ── Full-featured entity (exercises every FieldType and attribute) ───────────
[Resource("widget",
    Plural   = "widgets",
    Module   = "inventory",
    Icon     = "mat:widgets",
    Label    = "Widget",
    SortOrder = 1)]
[ResourceAction("activate",
    Label      = "Activate",
    Icon       = "mat:play_arrow",
    Permission = "widget:activate",
    Kind       = "row")]
public class WidgetEntity
{
    [Key]
    public long Id { get; set; }

    [Field(Label = "Widget Name", Required = true, InGrid = true, InForm = true, SortOrder = 1)]
    public string Name { get; set; } = "";

    [Field(InGrid = true, InForm = true, SortOrder = 2)]
    public int Count { get; set; }

    [Field(InGrid = true, InForm = true, SortOrder = 3)]
    public long BigCount { get; set; }

    [Field(InGrid = false, InForm = true, SortOrder = 4)]
    public decimal? Price { get; set; }          // nullable → Required inferred as false

    [Field(InGrid = true, InForm = true, SortOrder = 5)]
    public bool Active { get; set; }

    [Field(InGrid = true, InForm = true, SortOrder = 6)]
    public WidgetColor Color { get; set; }       // enum → enumValues populated

    [Field(SortOrder = 7)]
    public DateOnly MadeOn { get; set; }

    [Field(SortOrder = 8)]
    public DateTime UpdatedAt { get; set; }

    [HiddenField]
    public string? HiddenProperty { get; set; }  // must be excluded from fields

    [NotMapped]
    public string? ComputedProp => Name;          // must be excluded from fields
}

// ── Minimal entity: tests default Plural/Module/Label/Icon from scanner ──────
[Resource("sprocket")]
public class SprocketEntity
{
    [Key]
    public long Id { get; set; }

    public string? Description { get; set; }     // nullable NRT → Required = false
}

// ── Entity with navigation properties: tests Ref and RefCollection relations ─
[Resource("gadget", Plural = "gadgets", Module = "inventory", Label = "Gadget", SortOrder = 2)]
public class GadgetEntity
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = "";

    [Field(Label = "Primary Widget", RefResource = "widget")]
    public WidgetEntity? Widget { get; set; }               // Ref → one relation

    public ICollection<WidgetEntity> Components { get; set; } = []; // RefCollection → many relation
}

// ── No [Resource] attribute: must be ignored by the scanner ─────────────────
public class PlainEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

// ── Abstract + [Resource]: must be ignored because IsAbstract == true ────────
[Resource("abstract-widget")]
public abstract class AbstractResourceEntity
{
    public long Id { get; set; }
}
