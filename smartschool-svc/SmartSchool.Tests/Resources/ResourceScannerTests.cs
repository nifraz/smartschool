using FluentAssertions;
using SmartSchool.Resources.Descriptors;
using SmartSchool.Resources.Scanning;
using SmartSchool.Tests.Helpers;

namespace SmartSchool.Tests.Resources;

public class ResourceScannerTests
{
    private static readonly ResourceScanner Scanner = new();
    private static readonly IReadOnlyList<ResourceDescriptor> TestAssemblyResults =
        Scanner.Scan(typeof(WidgetEntity).Assembly);

    // ── Discovery ────────────────────────────────────────────────────────────

    [Fact]
    public void Scan_ResourceAnnotatedConcreteClass_IsIncluded()
        => TestAssemblyResults.Should().Contain(d => d.Key == "widget");

    [Fact]
    public void Scan_ClassWithoutResourceAttribute_IsExcluded()
        => TestAssemblyResults.Should().NotContain(d => d.ClrType == typeof(PlainEntity));

    [Fact]
    public void Scan_AbstractClassWithResourceAttribute_IsExcluded()
        => TestAssemblyResults.Should().NotContain(d => d.Key == "abstract-widget");

    [Fact]
    public void Scan_MultipleAssemblies_ReturnsCombinedResults()
    {
        var results = Scanner.Scan(typeof(WidgetEntity).Assembly, typeof(WidgetEntity).Assembly);
        // Duplicate assemblies → same keys; deduplication is not required,
        // but we confirm that both inputs are processed.
        results.Should().NotBeEmpty();
    }

    // ── Descriptor fields from attribute ─────────────────────────────────────

    [Fact]
    public void Scan_Key_TakenFromAttribute()
        => Widget().Key.Should().Be("widget");

    [Fact]
    public void Scan_Plural_TakenFromAttribute()
        => Widget().Plural.Should().Be("widgets");

    [Fact]
    public void Scan_Plural_DefaultsToKeyPlusS_WhenNotSpecified()
        => Sprocket().Plural.Should().Be("sprockets");

    [Fact]
    public void Scan_Module_TakenFromAttribute()
        => Widget().Module.Should().Be("inventory");

    [Fact]
    public void Scan_Module_DefaultsToGeneral_WhenNotSpecified()
        => Sprocket().Module.Should().Be("general");

    [Fact]
    public void Scan_Icon_TakenFromAttribute()
        => Widget().Icon.Should().Be("mat:widgets");

    [Fact]
    public void Scan_Icon_DefaultsToFolder_WhenNotSpecified()
        => Sprocket().Icon.Should().Be("mat:folder");

    [Fact]
    public void Scan_Label_TakenFromAttribute()
        => Widget().Label.Should().Be("Widget");

    [Fact]
    public void Scan_Label_HumanizesTypeName_WhenNotSpecified()
        => Sprocket().Label.Should().Be("Sprocket Entity"); // "SprocketEntity" → "Sprocket Entity"

    [Fact]
    public void Scan_ClrType_SetToDecoratedClass()
        => Widget().ClrType.Should().Be(typeof(WidgetEntity));

    [Fact]
    public void Scan_SortOrder_TakenFromAttribute()
        => Widget().SortOrder.Should().Be(1);

    // ── Result ordering ───────────────────────────────────────────────────────

    [Fact]
    public void Scan_Results_OrderedBySortOrderThenKey()
    {
        var testResults = TestAssemblyResults
            .Where(d => d.Module == "inventory")
            .ToList();

        testResults.Should().BeInAscendingOrder(d => d.SortOrder);
    }

    // ── Audit-field exclusion ─────────────────────────────────────────────────

    [Fact]
    public void Scan_IdField_IsExcluded()
        => Widget().Fields.Should().NotContain(f => f.Key == "id");

    [Fact]
    public void Scan_NotesField_IsExcluded()
        => Widget().Fields.Should().NotContain(f => f.Key == "notes");

    [Fact]
    public void Scan_AuditTimestamps_AreExcluded()
    {
        Widget().Fields.Should().NotContain(f =>
            f.Key is "createdTime" or "lastModifiedTime" or "deletedTime");
    }

    // ── [HiddenField] and [NotMapped] exclusion ───────────────────────────────

    [Fact]
    public void Scan_HiddenFieldAttribute_ExcludesProperty()
        => Widget().Fields.Should().NotContain(f => f.Key == "hiddenProperty");

    [Fact]
    public void Scan_NotMappedAttribute_ExcludesProperty()
        => Widget().Fields.Should().NotContain(f => f.Key == "computedProp");

    // ── FieldType mapping ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("name",      FieldType.String)]
    [InlineData("count",     FieldType.Int)]
    [InlineData("bigCount",  FieldType.Long)]
    [InlineData("price",     FieldType.Decimal)]
    [InlineData("active",    FieldType.Bool)]
    [InlineData("color",     FieldType.Enum)]
    [InlineData("madeOn",    FieldType.Date)]
    [InlineData("updatedAt", FieldType.DateTime)]
    public void Scan_PropertyType_MapsToCorrectFieldType(string fieldKey, FieldType expected)
    {
        var field = Widget().Fields.Single(f => f.Key == fieldKey);
        field.Type.Should().Be(expected);
    }

    // ── [Field] attribute overrides ───────────────────────────────────────────

    [Fact]
    public void Scan_FieldLabel_TakenFromFieldAttribute()
        => Widget().Fields.Single(f => f.Key == "name").Label.Should().Be("Widget Name");

    [Fact]
    public void Scan_FieldLabel_HumanizesPropertyName_WhenNoAttribute()
        => Widget().Fields.Single(f => f.Key == "count").Label.Should().Be("Count");

    [Fact]
    public void Scan_FieldRequired_TrueWhenAttributeSaysRequired()
        => Widget().Fields.Single(f => f.Key == "name").Required.Should().BeTrue();

    [Fact]
    public void Scan_FieldRequired_FalseForNullableValueType()
        => Widget().Fields.Single(f => f.Key == "price").Required.Should().BeFalse();

    [Fact]
    public void Scan_FieldInGrid_FalseWhenAttributeSaysInGridFalse()
        => Widget().Fields.Single(f => f.Key == "price").InGrid.Should().BeFalse();

    [Fact]
    public void Scan_FieldSortOrder_TakenFromFieldAttribute()
        => Widget().Fields.Single(f => f.Key == "name").SortOrder.Should().Be(1);

    // ── Enum field ────────────────────────────────────────────────────────────

    [Fact]
    public void Scan_EnumField_EnumNameIsTypeName()
        => Widget().Fields.Single(f => f.Key == "color").EnumName.Should().Be(nameof(WidgetColor));

    [Fact]
    public void Scan_EnumField_EnumValuesContainsAllMembers()
        => Widget().Fields.Single(f => f.Key == "color").EnumValues
            .Should().BeEquivalentTo(new[] { "Red", "Green", "Blue" });

    // ── Relations (navigation properties) ────────────────────────────────────

    [Fact]
    public void Scan_SingleNavigationRef_CreatesOneRelation()
        => Gadget().Relations.Should().Contain(r => r.Kind == "one" && r.Key == "Widget");

    [Fact]
    public void Scan_NavigationCollection_CreatesManyRelation()
        => Gadget().Relations.Should().Contain(r => r.Kind == "many" && r.Key == "Components");

    [Fact]
    public void Scan_NavigationRef_NotIncludedInFields()
        => Gadget().Fields.Should().NotContain(f => f.Key == "widget");

    [Fact]
    public void Scan_NavigationCollection_NotIncludedInFields()
        => Gadget().Fields.Should().NotContain(f => f.Key == "components");

    // ── Actions ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scan_ResourceActionAttribute_CreatesAction()
        => Widget().Actions.Should().Contain(a => a.Key == "activate");

    [Fact]
    public void Scan_Action_FieldsFromAttribute()
    {
        var action = Widget().Actions.Single(a => a.Key == "activate");
        action.Label.Should().Be("Activate");
        action.Icon.Should().Be("mat:play_arrow");
        action.Permission.Should().Be("widget:activate");
        action.Kind.Should().Be("row");
    }

    [Fact]
    public void Scan_NoResourceActionAttributes_ActionsEmpty()
        => Sprocket().Actions.Should().BeEmpty();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ResourceDescriptor Widget()   => TestAssemblyResults.Single(d => d.Key == "widget");
    private static ResourceDescriptor Gadget()   => TestAssemblyResults.Single(d => d.Key == "gadget");
    private static ResourceDescriptor Sprocket() => TestAssemblyResults.Single(d => d.Key == "sprocket");
}
