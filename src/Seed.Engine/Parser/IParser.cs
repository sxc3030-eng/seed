namespace Seed.Engine.Parser;

public interface IParser
{
    ParseResult Parse(string dsl);
}
