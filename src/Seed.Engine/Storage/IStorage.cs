using Seed.Engine.Models;

namespace Seed.Engine.Storage;

public interface IStorage
{
    string Save(DnaFile dna, string id);
    DnaFile Load(string id);
    IReadOnlyList<ProjectMetadata> List();
    void Delete(string id);
    bool Exists(string id);
}
