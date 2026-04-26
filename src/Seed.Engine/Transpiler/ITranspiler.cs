using Seed.Engine.Models;
using Seed.Engine.Parser;

namespace Seed.Engine.Transpiler;

public interface ITranspiler
{
    DnaFile Transpile(ParseResult parseResult);
}
