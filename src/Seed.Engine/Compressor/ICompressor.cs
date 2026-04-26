using Seed.Engine.Models;

namespace Seed.Engine.Compressor;

public interface ICompressor
{
    string Compress(DnaFile dna);
}
