using FileCompass.Core.Data;
using FileCompass.Core.Models;

namespace FileCompass.Tests;

public class TagRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly DatabaseService _db;
    private readonly TagRepository _tagRepo;
    private readonly LocationRepository _locationRepo;
    private readonly LocationTagRepository _locationTagRepo;

    public TagRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"filecompass_tag_test_{Guid.NewGuid()}.db");
        _db = new DatabaseService(_tempDbPath);
        _tagRepo = new TagRepository(_db);
        _locationRepo = new LocationRepository(_db);
        _locationTagRepo = new LocationTagRepository(_db);
    }

    #region TagRepository Tests

    [Fact]
    public async Task TagRepository_AddAndRetrieve()
    {
        await _db.InitializeAsync();

        var tag = new Tag { Name = "Important", Colour = "#FF0000" };
        var added = await _tagRepo.AddAsync(tag);

        Assert.True(added.Id > 0);

        var retrieved = await _tagRepo.GetByIdAsync(added.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Important", retrieved.Name);
        Assert.Equal("#FF0000", retrieved.Colour);
    }

    [Fact]
    public async Task TagRepository_GetByName()
    {
        await _db.InitializeAsync();

        await _tagRepo.AddAsync(new Tag { Name = "TestTag", Colour = "#00FF00" });

        var retrieved = await _tagRepo.GetByNameAsync("TestTag");
        Assert.NotNull(retrieved);
        Assert.Equal("TestTag", retrieved.Name);

        var caseInsensitive = await _tagRepo.GetByNameAsync("testtag");
        Assert.NotNull(caseInsensitive);
        Assert.Equal("TestTag", caseInsensitive.Name);
    }

    [Fact]
    public async Task TagRepository_GetAll_OrderedByName()
    {
        await _db.InitializeAsync();

        await _tagRepo.AddAsync(new Tag { Name = "Zebra", Colour = "#000000" });
        await _tagRepo.AddAsync(new Tag { Name = "Apple", Colour = "#FF0000" });
        await _tagRepo.AddAsync(new Tag { Name = "Mango", Colour = "#00FF00" });

        var all = await _tagRepo.GetAllAsync();

        Assert.Equal(3, all.Count);
        Assert.Equal("Apple", all[0].Name);
        Assert.Equal("Mango", all[1].Name);
        Assert.Equal("Zebra", all[2].Name);
    }

    [Fact]
    public async Task TagRepository_Update()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "OldName", Colour = "#000000" });
        tag.Name = "NewName";
        tag.Colour = "#FFFFFF";

        await _tagRepo.UpdateAsync(tag);

        var retrieved = await _tagRepo.GetByIdAsync(tag.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("NewName", retrieved.Name);
        Assert.Equal("#FFFFFF", retrieved.Colour);
    }

    [Fact]
    public async Task TagRepository_Delete()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "ToDelete", Colour = "#000000" });
        await _tagRepo.DeleteAsync(tag.Id);

        var retrieved = await _tagRepo.GetByIdAsync(tag.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task TagRepository_GetLocationCount()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var loc1 = await _locationRepo.AddAsync(new Location { Path = "/path1" });
        var loc2 = await _locationRepo.AddAsync(new Location { Path = "/path2" });

        await _locationTagRepo.AssignTagAsync(loc1.Id, tag.Id);
        await _locationTagRepo.AssignTagAsync(loc2.Id, tag.Id);

        var count = await _tagRepo.GetLocationCountAsync(tag.Id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task TagRepository_GetLocationCount_ZeroWhenNoAssignments()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Unused", Colour = "#000000" });

        var count = await _tagRepo.GetLocationCountAsync(tag.Id);
        Assert.Equal(0, count);
    }

    #endregion

    #region LocationTagRepository Tests

    [Fact]
    public async Task LocationTagRepository_AssignAndRetrieve()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Work", Colour = "#0000FF" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/work" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Single(tags);
        Assert.Equal("Work", tags[0].Name);
    }

    [Fact]
    public async Task LocationTagRepository_AssignMultipleTags()
    {
        await _db.InitializeAsync();

        var tag1 = await _tagRepo.AddAsync(new Tag { Name = "Tag1", Colour = "#FF0000" });
        var tag2 = await _tagRepo.AddAsync(new Tag { Name = "Tag2", Colour = "#00FF00" });
        var tag3 = await _tagRepo.AddAsync(new Tag { Name = "Tag3", Colour = "#0000FF" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag1.Id);
        await _locationTagRepo.AssignTagAsync(location.Id, tag2.Id);
        await _locationTagRepo.AssignTagAsync(location.Id, tag3.Id);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public async Task LocationTagRepository_AssignSameTagTwice_NoError()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);
        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Single(tags);
    }

    [Fact]
    public async Task LocationTagRepository_Unassign()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);
        await _locationTagRepo.UnassignTagAsync(location.Id, tag.Id);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Empty(tags);
    }

    [Fact]
    public async Task LocationTagRepository_GetLocationIdsForTag()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Shared", Colour = "#FF0000" });
        var loc1 = await _locationRepo.AddAsync(new Location { Path = "/path1" });
        var loc2 = await _locationRepo.AddAsync(new Location { Path = "/path2" });
        var loc3 = await _locationRepo.AddAsync(new Location { Path = "/path3" });

        await _locationTagRepo.AssignTagAsync(loc1.Id, tag.Id);
        await _locationTagRepo.AssignTagAsync(loc2.Id, tag.Id);

        var locationIds = await _locationTagRepo.GetLocationIdsForTagAsync(tag.Id);
        Assert.Equal(2, locationIds.Count);
        Assert.Contains(loc1.Id, locationIds);
        Assert.Contains(loc2.Id, locationIds);
        Assert.DoesNotContain(loc3.Id, locationIds);
    }

    [Fact]
    public async Task LocationTagRepository_SetTagsReplacesExisting()
    {
        await _db.InitializeAsync();

        var tag1 = await _tagRepo.AddAsync(new Tag { Name = "Tag1", Colour = "#FF0000" });
        var tag2 = await _tagRepo.AddAsync(new Tag { Name = "Tag2", Colour = "#00FF00" });
        var tag3 = await _tagRepo.AddAsync(new Tag { Name = "Tag3", Colour = "#0000FF" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag1.Id);
        await _locationTagRepo.SetTagsForLocationAsync(location.Id, [tag2.Id, tag3.Id]);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Equal(2, tags.Count);
        Assert.DoesNotContain(tags, t => string.Equals(t.Name, "Tag1", StringComparison.Ordinal));
        Assert.Contains(tags, t => string.Equals(t.Name, "Tag2", StringComparison.Ordinal));
        Assert.Contains(tags, t => string.Equals(t.Name, "Tag3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LocationTagRepository_SetTagsWithEmptyList_ClearsAll()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);
        await _locationTagRepo.SetTagsForLocationAsync(location.Id, []);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Empty(tags);
    }

    #endregion

    #region Cascade Delete Tests

    [Fact]
    public async Task CascadeDelete_LocationDeletion_RemovesTagAssignments()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);
        await _locationRepo.DeleteAsync(location.Id);

        var retrievedTag = await _tagRepo.GetByIdAsync(tag.Id);
        Assert.NotNull(retrievedTag);

        var count = await _tagRepo.GetLocationCountAsync(tag.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CascadeDelete_TagDeletion_RemovesTagAssignments()
    {
        await _db.InitializeAsync();

        var tag = await _tagRepo.AddAsync(new Tag { Name = "Test", Colour = "#000000" });
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        await _locationTagRepo.AssignTagAsync(location.Id, tag.Id);
        await _tagRepo.DeleteAsync(tag.Id);

        var retrievedLocation = await _locationRepo.GetByIdAsync(location.Id);
        Assert.NotNull(retrievedLocation);

        var tags = await _locationTagRepo.GetTagsForLocationAsync(location.Id);
        Assert.Empty(tags);
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _db.Dispose();
        if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
        var walPath = _tempDbPath + "-wal";
        var shmPath = _tempDbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);
    }
}
