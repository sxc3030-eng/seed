using FluentAssertions;
using Seed.Engine.Models;
using Seed.Engine.Storage;
using Xunit;

namespace Seed.Engine.Tests.Storage;

public class JsonFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly IStorage _storage;

    public JsonFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"seed-tests-{Guid.NewGuid():N}");
        _storage = new JsonFileStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static DnaFile MakeSample(string name = "x") => new()
    {
        Header = new ProjectHeader { Type = "cli", Name = name, Goal = "g" },
        Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
    };

    [Fact]
    public void Save_ThenLoad_RoundTripsAllData()
    {
        var dna = MakeSample();
        var id = _storage.Save(dna, "test-1");
        var loaded = _storage.Load(id);

        loaded.Header.Name.Should().Be("x");
        loaded.Statements.Should().HaveCount(1);
        loaded.Statements[0].Verb.Should().Be("filtrer");
    }

    [Fact]
    public void Save_CreatesFileOnDisk()
    {
        _storage.Save(MakeSample(), "disk-test");
        Directory.GetFiles(_root, "*.dna").Should().NotBeEmpty();
    }

    [Fact]
    public void List_AfterTwoSaves_ReturnsTwoEntries()
    {
        _storage.Save(MakeSample("p1"), "p1");
        _storage.Save(MakeSample("p2"), "p2");

        var entries = _storage.List();
        entries.Should().HaveCount(2);
        entries.Select(e => e.Name).Should().Contain(new[] { "p1", "p2" });
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        _storage.Save(MakeSample(), "to-delete");
        _storage.Exists("to-delete").Should().BeTrue();
        _storage.Delete("to-delete");
        _storage.Exists("to-delete").Should().BeFalse();
    }

    [Fact]
    public void Load_NonExistentId_ThrowsFileNotFoundException()
    {
        var act = () => _storage.Load("never-existed");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        _storage.Save(MakeSample("original"), "same-id");
        _storage.Save(MakeSample("updated"), "same-id");
        _storage.Load("same-id").Header.Name.Should().Be("updated");
    }
}
