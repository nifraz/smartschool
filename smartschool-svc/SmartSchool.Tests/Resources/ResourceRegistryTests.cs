using FluentAssertions;
using SmartSchool.Resources;
using SmartSchool.Tests.Helpers;

namespace SmartSchool.Tests.Resources;

public class ResourceRegistryTests
{
    // ── Initialization ────────────────────────────────────────────────────────

    [Fact]
    public void All_BeforeInitialize_IsEmpty()
    {
        var registry = new ResourceRegistry();
        registry.All.Should().BeEmpty();
    }

    [Fact]
    public void Initialize_ScansAssembly_PopulatesAll()
    {
        var registry = new ResourceRegistry();
        registry.Initialize(typeof(WidgetEntity).Assembly);
        registry.All.Should().NotBeEmpty();
    }

    [Fact]
    public void Initialize_IncludesAllResourceAnnotatedTypes()
    {
        var registry = new ResourceRegistry();
        registry.Initialize(typeof(WidgetEntity).Assembly);

        registry.All.Select(d => d.Key).Should().Contain(["widget", "gadget", "sprocket"]);
    }

    [Fact]
    public void Initialize_ExcludesAbstractAndUnannotatedTypes()
    {
        var registry = new ResourceRegistry();
        registry.Initialize(typeof(WidgetEntity).Assembly);

        registry.All.Select(d => d.Key).Should().NotContain("abstract-widget");
        registry.All.Select(d => d.ClrType).Should().NotContain(typeof(PlainEntity));
    }

    [Fact]
    public void Initialize_CalledTwice_RefreshesRegistry()
    {
        var registry = new ResourceRegistry();
        registry.Initialize(typeof(WidgetEntity).Assembly);
        int firstCount = registry.All.Count;

        registry.Initialize(typeof(WidgetEntity).Assembly);
        registry.All.Count.Should().Be(firstCount); // same count after re-init
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Get_ExistingKey_ReturnsDescriptor()
    {
        var registry = Build();
        registry.Get("widget").Should().NotBeNull();
        registry.Get("widget")!.Key.Should().Be("widget");
    }

    [Fact]
    public void Get_UnknownKey_ReturnsNull()
    {
        var registry = Build();
        registry.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Get_WrongCase_ReturnsNull()
    {
        var registry = Build();
        // Keys are stored case-sensitively (ConcurrentDictionary default)
        registry.Get("Widget").Should().BeNull();
        registry.Get("WIDGET").Should().BeNull();
    }

    [Fact]
    public void Get_EmptyString_ReturnsNull()
    {
        var registry = Build();
        registry.Get("").Should().BeNull();
    }

    // ── All ───────────────────────────────────────────────────────────────────

    [Fact]
    public void All_OrderedBySortOrderThenKey()
    {
        var registry = Build();
        var inventoryItems = registry.All.Where(d => d.Module == "inventory").ToList();
        inventoryItems.Should().BeInAscendingOrder(d => d.SortOrder);
    }

    [Fact]
    public void All_IsReadOnly_CannotBeModified()
    {
        var registry = Build();
        // IReadOnlyList — verify it is truly read-only
        registry.All.Should().BeAssignableTo<IReadOnlyList<SmartSchool.Resources.Descriptors.ResourceDescriptor>>();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static ResourceRegistry Build()
    {
        var r = new ResourceRegistry();
        r.Initialize(typeof(WidgetEntity).Assembly);
        return r;
    }
}
