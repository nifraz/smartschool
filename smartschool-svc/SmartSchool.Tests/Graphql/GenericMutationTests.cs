using System.Text.Json;
using FluentAssertions;
using HotChocolate;
using SmartSchool.Graphql.Mutations;
using SmartSchool.Resources;
using SmartSchool.Schema.Entities;
using SmartSchool.Schema.Enums;
using SmartSchool.Tests.Helpers;

namespace SmartSchool.Tests.Graphql;

public class GenericMutationTests
{
    // ── createResource ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateResource_UnknownResource_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();
        var mutation = new GenericMutation();

        var act = () => mutation.CreateResourceAsync(
            "nonexistent", Json(new { name = "x" }), SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public async Task CreateResource_ValidInput_ReturnsNonZeroId()
    {
        using var db = TestDb.Create();

        var id = await new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { censusNo = "S001", name = "New School",
                       location = "Colombo", type = "Government", divisionId = 1 }),
            SchoolRegistry(), db, default);

        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateResource_EntityPersistedInDatabase()
    {
        using var db = TestDb.Create();

        await new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { censusNo = "S100", name = "Persisted School",
                       location = "Galle", type = "Government", divisionId = 1 }),
            SchoolRegistry(), db, default);

        db.Schools.Should().ContainSingle(s => s.Name == "Persisted School");
    }

    [Fact]
    public async Task CreateResource_CamelCaseInput_MapsToEntityProperties()
    {
        using var db = TestDb.Create();

        await new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { censusNo = "X99", name = "CamelCase School",
                       location = "Kandy", type = "Government", divisionId = 2 }),
            SchoolRegistry(), db, default);

        var school = db.Schools.Single();
        school.CensusNo.Should().Be("X99");
        school.Name.Should().Be("CamelCase School");
        school.Location.Should().Be("Kandy");
    }

    [Fact]
    public async Task CreateResource_PascalCaseInput_AlsoMaps()
    {
        using var db = TestDb.Create();

        await new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { CensusNo = "Y99", Name = "Pascal School",
                       Location = "Matara", Type = "Government", DivisionId = 1 }),
            SchoolRegistry(), db, default);

        db.Schools.Single().Name.Should().Be("Pascal School");
    }

    [Fact]
    public async Task CreateResource_UnknownInputProperty_IsIgnored()
    {
        using var db = TestDb.Create();

        // Should not throw even though "doesNotExist" has no matching property
        var act = () => new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { censusNo = "Z01", name = "School",
                       location = "Jaffna", type = "Government",
                       divisionId = 1, doesNotExist = "boom" }),
            SchoolRegistry(), db, default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateResource_NonObjectJson_DoesNotThrow()
    {
        using var db = TestDb.Create();

        // Passing a JSON array instead of an object: Apply() returns early
        var jsonArray = JsonSerializer.Deserialize<JsonElement>("[1,2,3]");

        var act = () => new GenericMutation()
            .CreateResourceAsync("school", jsonArray, SchoolRegistry(), db, default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateResource_WrongTypeForProperty_IsIgnored()
    {
        using var db = TestDb.Create();

        // DivisionId is int; passing a string that can't parse should be silently ignored
        var act = () => new GenericMutation().CreateResourceAsync(
            "school",
            Json(new { censusNo = "E01", name = "Error Test",
                       location = "X", type = "Government",
                       divisionId = "not-a-number" }),
            SchoolRegistry(), db, default);

        await act.Should().NotThrowAsync();
    }

    // ── updateResource ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateResource_UnknownResource_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();

        var act = () => new GenericMutation()
            .UpdateResourceAsync("nonexistent", 1, Json(new { }), SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public async Task UpdateResource_EntityNotFound_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();

        var act = () => new GenericMutation()
            .UpdateResourceAsync("school", 9999, Json(new { name = "x" }),
                SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*Not found*");
    }

    [Fact]
    public async Task UpdateResource_ValidInput_UpdatesEntityAndReturnsTrue()
    {
        using var db = TestDb.Create();
        db.Schools.Add(new School
        {
            CensusNo = "U01", Name = "Before", Location = "A",
            Type = SchoolType.Government, DivisionId = 1
        });
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        var result = await new GenericMutation()
            .UpdateResourceAsync("school", id, Json(new { name = "After" }),
                SchoolRegistry(), db, default);

        result.Should().BeTrue();
        db.Schools.Find(id)!.Name.Should().Be("After");
    }

    [Fact]
    public async Task UpdateResource_PartialInput_OnlyUpdatesSpecifiedFields()
    {
        using var db = TestDb.Create();
        db.Schools.Add(new School
        {
            CensusNo = "P01", Name = "Original", Location = "Original Location",
            Type = SchoolType.Government, DivisionId = 1
        });
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        await new GenericMutation()
            .UpdateResourceAsync("school", id, Json(new { name = "Changed" }),
                SchoolRegistry(), db, default);

        var updated = db.Schools.Find(id)!;
        updated.Name.Should().Be("Changed");
        updated.Location.Should().Be("Original Location"); // untouched
    }

    // ── deleteResource ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteResource_UnknownResource_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();

        var act = () => new GenericMutation()
            .DeleteResourceAsync("nonexistent", 1, SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public async Task DeleteResource_EntityNotFound_ThrowsGraphQLException()
    {
        using var db = TestDb.Create();

        var act = () => new GenericMutation()
            .DeleteResourceAsync("school", 9999, SchoolRegistry(), db, default);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*Not found*");
    }

    [Fact]
    public async Task DeleteResource_ValidId_ReturnsTrue()
    {
        using var db = TestDb.Create();
        db.Schools.Add(new School
        {
            CensusNo = "D01", Name = "To Delete", Location = "X",
            Type = SchoolType.Government, DivisionId = 1
        });
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        var result = await new GenericMutation()
            .DeleteResourceAsync("school", id, SchoolRegistry(), db, default);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteResource_SoftDeletesSetsDeletedTime()
    {
        using var db = TestDb.Create();
        db.Schools.Add(new School
        {
            CensusNo = "D02", Name = "Soft Delete Test", Location = "X",
            Type = SchoolType.Government, DivisionId = 1
        });
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        await new GenericMutation()
            .DeleteResourceAsync("school", id, SchoolRegistry(), db, default);

        var deleted = db.Schools.IgnoreQueryFilters().Single(s => s.Id == id);
        deleted.DeletedTime.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteResource_SoftDeletedEntity_HiddenFromSubsequentQueries()
    {
        using var db = TestDb.Create();
        db.Schools.Add(new School
        {
            CensusNo = "D03", Name = "Hidden After Delete", Location = "X",
            Type = SchoolType.Government, DivisionId = 1
        });
        await db.SaveChangesAsync();
        var id = db.Schools.First().Id;

        await new GenericMutation()
            .DeleteResourceAsync("school", id, SchoolRegistry(), db, default);

        db.Schools.Should().BeEmpty(); // global filter hides it
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JsonElement Json(object obj)
        => JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(obj));

    private static ResourceRegistry SchoolRegistry()
    {
        var r = new ResourceRegistry();
        r.Initialize(typeof(School).Assembly);
        return r;
    }
}
