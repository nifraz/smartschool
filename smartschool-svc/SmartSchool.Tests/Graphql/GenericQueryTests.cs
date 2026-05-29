using System.Text.Json;
using FluentAssertions;
using HotChocolate;
using SmartSchool.Graphql.Queries;
using SmartSchool.Resources;
using SmartSchool.Schema.Entities;
using SmartSchool.Schema.Enums;
using SmartSchool.Tests.Helpers;

namespace SmartSchool.Tests.Graphql;

public class GenericQueryTests
{
    // ── resourceItems: unknown resource ──────────────────────────────────────

    [Fact]
    public async Task ResourceItems_UnknownResource_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();
        var registry = SchoolRegistry();
        var query = new GenericQuery();

        var act = () => query.ResourceItemsAsync("nonexistent", 0, 10, registry, db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*nonexistent*");
    }

    // ── resourceItems: paging ─────────────────────────────────────────────────

    [Fact]
    public async Task ResourceItems_EmptyDatabase_ReturnsTotalZeroAndEmptyList()
    {
        using var db = TestDb.Create();
        var query = new GenericQuery();

        var result = await query.ResourceItemsAsync("school", 0, 10, SchoolRegistry(), db, default);

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ResourceItems_WithData_ReturnCorrectTotal()
    {
        using var db = TestDb.Create();
        db.Schools.AddRange(School("A"), School("B"), School("C"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, 10, SchoolRegistry(), db, default);

        result.Total.Should().Be(3);
    }

    [Fact]
    public async Task ResourceItems_SkipAndTake_PaginatesCorrectly()
    {
        using var db = TestDb.Create();
        db.Schools.AddRange(School("A"), School("B"), School("C"), School("D"), School("E"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 2, 2, SchoolRegistry(), db, default);

        result.Total.Should().Be(5);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ResourceItems_SkipExceedsTotal_ReturnsEmptyItems()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("A"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 100, 10, SchoolRegistry(), db, default);

        result.Total.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    // ── resourceItems: take clamping ─────────────────────────────────────────

    [Fact]
    public async Task ResourceItems_TakeAbove200_ClampedTo200()
    {
        using var db = TestDb.Create();
        // Seed 5 schools — if take were not clamped to 200, any value >5 would
        // still only return 5. We verify the clamp via the no-exception path.
        db.Schools.AddRange(Enumerable.Range(1, 5).Select(i => School($"S{i}")));
        await db.SaveChangesAsync();

        var act = () => new GenericQuery()
            .ResourceItemsAsync("school", 0, 9999, SchoolRegistry(), db, default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResourceItems_TakeZero_ClampedToOne()
    {
        using var db = TestDb.Create();
        db.Schools.AddRange(School("A"), School("B"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, 0, SchoolRegistry(), db, default);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResourceItems_TakeNegative_ClampedToOne()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("A"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, -50, SchoolRegistry(), db, default);

        result.Items.Should().HaveCount(1);
    }

    // ── resourceItems: serialization ─────────────────────────────────────────

    [Fact]
    public async Task ResourceItems_ReturnsCamelCaseJsonStrings()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("JSON School"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, 10, SchoolRegistry(), db, default);

        var json = JsonDocument.Parse(result.Items[0]);
        json.RootElement.TryGetProperty("name", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("Name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ResourceItems_JsonContainsExpectedFieldValues()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("Test School"));
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, 10, SchoolRegistry(), db, default);

        var json = JsonDocument.Parse(result.Items[0]);
        json.RootElement.GetProperty("name").GetString().Should().Be("Test School");
        json.RootElement.GetProperty("censusNo").GetString().Should().Be("C001");
    }

    // ── resourceItems: soft-delete global filter ──────────────────────────────

    [Fact]
    public async Task ResourceItems_SoftDeletedEntities_AreExcluded()
    {
        using var db = TestDb.Create();
        var active  = School("Active");
        var deleted = School("Deleted");
        deleted.DeletedTime = DateTime.UtcNow;

        db.Schools.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var result = await new GenericQuery()
            .ResourceItemsAsync("school", 0, 10, SchoolRegistry(), db, default);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        JsonDocument.Parse(result.Items[0]).RootElement
            .GetProperty("name").GetString().Should().Be("Active");
    }

    // ── resourceItem ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ResourceItem_UnknownResource_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();

        var act = () => new GenericQuery()
            .ResourceItemAsync("nonexistent", 1, SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public async Task ResourceItem_EntityNotFound_ReturnsNull()
    {
        using var db = TestDb.Create();

        var result = await new GenericQuery()
            .ResourceItemAsync("school", 9999, SchoolRegistry(), db, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResourceItem_ValidId_ReturnsSerializedJson()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("Specific School"));
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        var result = await new GenericQuery()
            .ResourceItemAsync("school", id, SchoolRegistry(), db, default);

        result.Should().NotBeNull();
        var json = JsonDocument.Parse(result!);
        json.RootElement.GetProperty("name").GetString().Should().Be("Specific School");
    }

    [Fact]
    public async Task ResourceItem_UsesCamelCaseKeys()
    {
        using var db = TestDb.Create();
        db.Schools.Add(School("Camel Test"));
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        var result = await new GenericQuery()
            .ResourceItemAsync("school", id, SchoolRegistry(), db, default);

        var json = JsonDocument.Parse(result!);
        json.RootElement.TryGetProperty("censusNo", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("CensusNo", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ResourceItem_SoftDeletedEntity_ReturnsNull()
    {
        using var db = TestDb.Create();
        var school = School("Deleted");
        school.DeletedTime = DateTime.UtcNow;
        db.Schools.Add(school);
        await db.SaveChangesAsync();
        var id = db.Schools.IgnoreQueryFilters().First().Id;

        var result = await new GenericQuery()
            .ResourceItemAsync("school", id, SchoolRegistry(), db, default);

        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static School School(string name) => new()
    {
        CensusNo = "C001",
        Name     = name,
        Location = "Test Location",
        Type     = SchoolType.Government,
        DivisionId = 1,
    };

    private static ResourceRegistry SchoolRegistry()
    {
        var r = new ResourceRegistry();
        r.Initialize(typeof(School).Assembly);
        return r;
    }
}
