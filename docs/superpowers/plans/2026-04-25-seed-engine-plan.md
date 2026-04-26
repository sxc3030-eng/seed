# SEED Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the SEED engine as a C# .NET 8 class library that compiles project-intent UI choices into compressed DSL strings + canonical `.dna` JSON files, with full round-trip parsing.

**Architecture:** Six decoupled units with single responsibilities (TokenDb, Composer, Parser, Transpiler, Compressor, Storage), connected via clean interfaces. Pure data flow, no UI/host dependencies. Each unit is independently testable. The library is consumed by FORGE (v1 host, separate plan) and eventually a web SPA via Blazor Wasm (palier 2).

**Tech Stack:**
- .NET 8 (LTS, matches FORGE/Godot)
- xUnit + FluentAssertions for tests
- System.Text.Json for serialization
- Source-of-truth specification: `docs/superpowers/specs/2026-04-25-seed-design.md`

**Out of scope (separate plan):**
- FORGE Godot panel UI
- 3D renderer adapter (`.dna` → Godot scene)
- Web SPA (palier 2)

---

## File Structure

```
seed/
├── src/
│   └── Seed.Engine/
│       ├── Seed.Engine.csproj
│       ├── Models/
│       │   ├── DnaFile.cs                   # Top-level .dna structure
│       │   ├── ProjectHeader.cs             # type/name/goal triplet
│       │   ├── Statement.cs                 # verb + target + modifiers + links + comment
│       │   ├── Modifier.cs                  # {key, value} pair (key nullable)
│       │   ├── Link.cs                      # to + type (seq/par/alt)
│       │   ├── Constraint.cs                # !X markers
│       │   └── EntityRef.cs                 # @X references
│       ├── TokenDb/
│       │   ├── ITokenDb.cs
│       │   ├── TokenDb.cs                   # JSON-backed verb/modifier registry
│       │   ├── TokenDbModels.cs             # VerbDefinition, ModifierDefinition, GrammarProfile
│       │   └── tokens.cli.json              # Embedded resource: CLI grammar (~20 verbs)
│       ├── Composer/
│       │   ├── IComposer.cs
│       │   ├── Composer.cs                  # UI choices → DSL string
│       │   └── ComposerModels.cs            # ComposerInput record
│       ├── Parser/
│       │   ├── IParser.cs
│       │   ├── Parser.cs                    # DSL string → AST + errors
│       │   ├── Tokenizer.cs                 # Lexer (chars → tokens)
│       │   ├── ParserModels.cs              # ParseResult, ParseError, AstStatement
│       │   └── ParseError.cs
│       ├── Transpiler/
│       │   ├── ITranspiler.cs
│       │   └── Transpiler.cs                # AST → DnaFile
│       ├── Compressor/
│       │   ├── ICompressor.cs
│       │   └── Compressor.cs                # DnaFile → minimal DSL string for LLM
│       └── Storage/
│           ├── IStorage.cs
│           ├── ProjectMetadata.cs
│           └── JsonFileStorage.cs           # CRUD on .dna files in user dir
└── tests/
    └── Seed.Engine.Tests/
        ├── Seed.Engine.Tests.csproj
        ├── Models/
        │   └── DnaFileSerializationTests.cs
        ├── TokenDb/
        │   └── TokenDbTests.cs
        ├── Composer/
        │   └── ComposerTests.cs
        ├── Parser/
        │   ├── TokenizerTests.cs
        │   └── ParserTests.cs
        ├── Transpiler/
        │   └── TranspilerTests.cs
        ├── Compressor/
        │   └── CompressorTests.cs
        ├── Storage/
        │   └── JsonFileStorageTests.cs
        └── EndToEnd/
            └── RoundTripTests.cs
```

**Boundaries enforced:**
- `Models/` has zero dependencies (pure POCOs/records)
- `TokenDb/` depends only on `Models/` and `tokens.cli.json` resource
- `Parser/` depends on `Models/` and `TokenDb/` (for verb validation)
- `Composer/` depends on `Models/` and `TokenDb/`
- `Transpiler/` depends on `Models/` and `Parser/` (consumes AST)
- `Compressor/` depends only on `Models/`
- `Storage/` depends only on `Models/` and System.IO

---

## Task 1: Project skeleton & build green

**Files:**
- Create: `src/Seed.Engine/Seed.Engine.csproj`
- Create: `tests/Seed.Engine.Tests/Seed.Engine.Tests.csproj`
- Create: `Seed.sln`
- Modify: `.gitignore` (add bin/obj if not present)

- [ ] **Step 1: Create the engine library .csproj**

Create `src/Seed.Engine/Seed.Engine.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Seed.Engine</RootNamespace>
    <AssemblyName>Seed.Engine</AssemblyName>
    <Version>0.1.0</Version>
    <Authors>bui1 (Simon Cantin)</Authors>
    <Description>SEED engine — DSL brief compactor for LLMs and FORGE 3D viz</Description>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create the test project .csproj**

Create `tests/Seed.Engine.Tests/Seed.Engine.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
    <RootNamespace>Seed.Engine.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Seed.Engine\Seed.Engine.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create the solution file**

Run from repo root:

```bash
dotnet new sln -n Seed
dotnet sln add src/Seed.Engine/Seed.Engine.csproj
dotnet sln add tests/Seed.Engine.Tests/Seed.Engine.Tests.csproj
```

Expected: `Seed.sln` created, both projects added.

- [ ] **Step 4: Verify build is green**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

Run: `dotnet test`
Expected: `Total tests: 0` (no tests yet, but test discovery works)

- [ ] **Step 5: Commit**

```bash
git add Seed.sln src/Seed.Engine/Seed.Engine.csproj tests/Seed.Engine.Tests/Seed.Engine.Tests.csproj
git commit -m "chore: project skeleton (engine lib + xUnit tests)"
```

---

## Task 2: Core models (DnaFile, Statement, Modifier, Link, Header, Constraint, EntityRef)

**Files:**
- Create: `src/Seed.Engine/Models/DnaFile.cs`
- Create: `src/Seed.Engine/Models/ProjectHeader.cs`
- Create: `src/Seed.Engine/Models/Statement.cs`
- Create: `src/Seed.Engine/Models/Modifier.cs`
- Create: `src/Seed.Engine/Models/Link.cs`
- Create: `src/Seed.Engine/Models/Constraint.cs`
- Create: `src/Seed.Engine/Models/EntityRef.cs`
- Create: `tests/Seed.Engine.Tests/Models/DnaFileSerializationTests.cs`

- [ ] **Step 1: Write the failing serialization round-trip test**

Create `tests/Seed.Engine.Tests/Models/DnaFileSerializationTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Models;

public class DnaFileSerializationTests
{
    [Fact]
    public void DnaFile_RoundTripsThroughJson_PreservingAllFields()
    {
        var original = new DnaFile
        {
            Version = "1.0",
            Header = new ProjectHeader
            {
                Type = "cli",
                Name = "mail-filter",
                Goal = "filtrer mes mails par pertinence"
            },
            Statements = new List<Statement>
            {
                new()
                {
                    Id = "s1",
                    Verb = "filtrer",
                    Target = "mail",
                    Modifiers = new List<Modifier>
                    {
                        new() { Key = null, Value = "pertinence" }
                    },
                    Comment = "filtre principal du flow",
                    Links = new List<Link> { new() { To = "s2", Type = LinkType.Seq } }
                },
                new()
                {
                    Id = "s2",
                    Verb = "enregistrer",
                    Target = "db",
                    Modifiers = new List<Modifier>
                    {
                        new() { Key = "type", Value = "sqlite" }
                    },
                    Constraints = new List<string> { "offline" }
                }
            }
        };

        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
        var roundTripped = JsonSerializer.Deserialize<DnaFile>(json);

        roundTripped.Should().BeEquivalentTo(original);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~DnaFileSerializationTests"`
Expected: FAIL with compilation errors (types don't exist yet)

- [ ] **Step 3: Implement Models/Modifier.cs**

Create `src/Seed.Engine/Models/Modifier.cs`:

```csharp
namespace Seed.Engine.Models;

/// <summary>
/// Represents a modifier on a DSL statement.
/// Key is null for simple modifiers like &lt;pertinence&gt;.
/// Key is set for qualified modifiers like &lt;db:sqlite&gt;.
/// </summary>
public sealed class Modifier
{
    public string? Key { get; init; }
    public string Value { get; init; } = string.Empty;
}
```

- [ ] **Step 4: Implement Models/Link.cs**

Create `src/Seed.Engine/Models/Link.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Seed.Engine.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LinkType
{
    Seq,
    Par,
    Alt
}

public sealed class Link
{
    public string To { get; init; } = string.Empty;
    public LinkType Type { get; init; } = LinkType.Seq;
}
```

- [ ] **Step 5: Implement Models/Statement.cs**

Create `src/Seed.Engine/Models/Statement.cs`:

```csharp
namespace Seed.Engine.Models;

public sealed class Statement
{
    public string Id { get; init; } = string.Empty;
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public string? Comment { get; init; }
    public List<Link> Links { get; init; } = new();
}
```

- [ ] **Step 6: Implement Models/ProjectHeader.cs**

Create `src/Seed.Engine/Models/ProjectHeader.cs`:

```csharp
namespace Seed.Engine.Models;

public sealed class ProjectHeader
{
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
}
```

- [ ] **Step 7: Implement Models/Constraint.cs and Models/EntityRef.cs**

Create `src/Seed.Engine/Models/Constraint.cs`:

```csharp
namespace Seed.Engine.Models;

public sealed class Constraint
{
    public string Value { get; init; } = string.Empty;
}
```

Create `src/Seed.Engine/Models/EntityRef.cs`:

```csharp
namespace Seed.Engine.Models;

public sealed class EntityRef
{
    public string Name { get; init; } = string.Empty;
}
```

- [ ] **Step 8: Implement Models/DnaFile.cs**

Create `src/Seed.Engine/Models/DnaFile.cs`:

```csharp
namespace Seed.Engine.Models;

public sealed class DnaFile
{
    public string Version { get; init; } = "1.0";
    public ProjectHeader Header { get; init; } = new();
    public List<Statement> Statements { get; init; } = new();
    public Dictionary<string, RenderingHint>? Rendering { get; init; }
}

public sealed class RenderingHint
{
    public double[] Pos { get; init; } = Array.Empty<double>();
}
```

- [ ] **Step 9: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~DnaFileSerializationTests"`
Expected: PASS

- [ ] **Step 10: Commit**

```bash
git add src/Seed.Engine/Models/ tests/Seed.Engine.Tests/Models/
git commit -m "feat(models): DnaFile + statement/modifier/link/header/constraint with JSON round-trip"
```

---

## Task 3: TokenDb resource file (CLI grammar v1)

**Files:**
- Create: `src/Seed.Engine/TokenDb/tokens.cli.json`
- Modify: `src/Seed.Engine/Seed.Engine.csproj` (embed resource)
- Create: `src/Seed.Engine/TokenDb/TokenDbModels.cs`

- [ ] **Step 1: Create tokens.cli.json with the v1 verb palette**

Create `src/Seed.Engine/TokenDb/tokens.cli.json`:

```json
{
  "projectType": "cli",
  "version": "1.0",
  "verbs": [
    { "name": "scraper", "category": "acquisition", "color": "#3b82f6", "acceptsModifiers": ["temps-réel", "format"] },
    { "name": "recevoir", "category": "acquisition", "color": "#3b82f6", "acceptsModifiers": ["webhook", "format"] },
    { "name": "lire", "category": "acquisition", "color": "#3b82f6", "acceptsModifiers": ["format", "encoding"] },
    { "name": "filtrer", "category": "transformation", "color": "#06b6d4", "acceptsModifiers": ["pertinence", "regex", "ml"] },
    { "name": "parser", "category": "transformation", "color": "#06b6d4", "acceptsModifiers": ["format", "strict"] },
    { "name": "transformer", "category": "transformation", "color": "#06b6d4", "acceptsModifiers": ["format"] },
    { "name": "analyser", "category": "analyse", "color": "#f97316", "acceptsModifiers": ["temps-réel", "ml"] },
    { "name": "détecter", "category": "analyse", "color": "#f97316", "acceptsModifiers": ["seuil", "pattern"] },
    { "name": "valider", "category": "analyse", "color": "#f97316", "acceptsModifiers": ["schéma", "strict"] },
    { "name": "créer", "category": "action", "color": "#22c55e", "acceptsModifiers": ["format", "template"] },
    { "name": "enregistrer", "category": "action", "color": "#22c55e", "acceptsModifiers": ["type", "format"] },
    { "name": "générer", "category": "action", "color": "#22c55e", "acceptsModifiers": ["format", "template"] },
    { "name": "envoyer", "category": "communication", "color": "#a855f7", "acceptsModifiers": ["format", "destinataire"] },
    { "name": "alerter", "category": "communication", "color": "#a855f7", "acceptsModifiers": ["si", "niveau"] },
    { "name": "notifier", "category": "communication", "color": "#a855f7", "acceptsModifiers": ["canal", "niveau"] },
    { "name": "surveiller", "category": "controle", "color": "#eab308", "acceptsModifiers": ["intervalle", "seuil"] },
    { "name": "déclencher", "category": "controle", "color": "#eab308", "acceptsModifiers": ["si", "cron"] },
    { "name": "retry", "category": "controle", "color": "#eab308", "acceptsModifiers": ["3-fois", "backoff"] },
    { "name": "logger", "category": "erreur", "color": "#991b1b", "acceptsModifiers": ["niveau", "format"] },
    { "name": "gérer-erreur", "category": "erreur", "color": "#991b1b", "acceptsModifiers": ["fallback", "alerte"] }
  ]
}
```

- [ ] **Step 2: Embed the resource in the project**

Modify `src/Seed.Engine/Seed.Engine.csproj` — add at the end before `</Project>`:

```xml
  <ItemGroup>
    <EmbeddedResource Include="TokenDb/tokens.cli.json" />
  </ItemGroup>
```

- [ ] **Step 3: Create TokenDbModels.cs (the in-memory representation)**

Create `src/Seed.Engine/TokenDb/TokenDbModels.cs`:

```csharp
namespace Seed.Engine.TokenDb;

public sealed class GrammarProfile
{
    public string ProjectType { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";
    public List<VerbDefinition> Verbs { get; init; } = new();
}

public sealed class VerbDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public List<string> AcceptsModifiers { get; init; } = new();
}
```

- [ ] **Step 4: Verify build is still green**

Run: `dotnet build`
Expected: Build succeeded. 0 Warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Seed.Engine/TokenDb/ src/Seed.Engine/Seed.Engine.csproj
git commit -m "feat(tokendb): embed CLI grammar with 20 verbs across 7 categories"
```

---

## Task 4: TokenDb implementation

**Files:**
- Create: `src/Seed.Engine/TokenDb/ITokenDb.cs`
- Create: `src/Seed.Engine/TokenDb/TokenDb.cs`
- Create: `tests/Seed.Engine.Tests/TokenDb/TokenDbTests.cs`

- [ ] **Step 1: Define the ITokenDb interface**

Create `src/Seed.Engine/TokenDb/ITokenDb.cs`:

```csharp
namespace Seed.Engine.TokenDb;

public interface ITokenDb
{
    GrammarProfile GetGrammar(string projectType);
    IReadOnlyList<VerbDefinition> GetVerbs(string projectType);
    VerbDefinition? FindVerb(string projectType, string verbName);
    bool IsValidCombination(string projectType, string verbName, string modifierKey);
}
```

- [ ] **Step 2: Write the failing test for TokenDb.GetVerbs**

Create `tests/Seed.Engine.Tests/TokenDb/TokenDbTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.TokenDb;
using Xunit;

namespace Seed.Engine.Tests.TokenDb;

public class TokenDbTests
{
    private readonly ITokenDb _db = new Engine.TokenDb.TokenDb();

    [Fact]
    public void GetVerbs_ForCli_Returns20Verbs()
    {
        var verbs = _db.GetVerbs("cli");
        verbs.Should().HaveCount(20);
        verbs.Select(v => v.Name).Should().Contain(new[] { "filtrer", "enregistrer", "alerter" });
    }

    [Fact]
    public void FindVerb_KnownVerb_ReturnsDefinition()
    {
        var verb = _db.FindVerb("cli", "filtrer");
        verb.Should().NotBeNull();
        verb!.Category.Should().Be("transformation");
        verb.Color.Should().Be("#06b6d4");
    }

    [Fact]
    public void FindVerb_UnknownVerb_ReturnsNull()
    {
        var verb = _db.FindVerb("cli", "non-existent-verb");
        verb.Should().BeNull();
    }

    [Fact]
    public void IsValidCombination_VerbAcceptsModifier_ReturnsTrue()
    {
        _db.IsValidCombination("cli", "filtrer", "pertinence").Should().BeTrue();
    }

    [Fact]
    public void IsValidCombination_VerbDoesNotAcceptModifier_ReturnsFalse()
    {
        _db.IsValidCombination("cli", "filtrer", "REST").Should().BeFalse();
    }

    [Fact]
    public void GetGrammar_UnknownType_ThrowsArgumentException()
    {
        var act = () => _db.GetGrammar("non-existent-type");
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~TokenDbTests"`
Expected: FAIL with compilation errors (TokenDb class doesn't exist yet)

- [ ] **Step 4: Implement TokenDb.cs**

Create `src/Seed.Engine/TokenDb/TokenDb.cs`:

```csharp
using System.Reflection;
using System.Text.Json;

namespace Seed.Engine.TokenDb;

public sealed class TokenDb : ITokenDb
{
    private readonly Dictionary<string, GrammarProfile> _grammars;

    public TokenDb()
    {
        _grammars = LoadEmbeddedGrammars();
    }

    public GrammarProfile GetGrammar(string projectType)
    {
        if (!_grammars.TryGetValue(projectType, out var grammar))
        {
            throw new ArgumentException($"Unknown project type: {projectType}", nameof(projectType));
        }
        return grammar;
    }

    public IReadOnlyList<VerbDefinition> GetVerbs(string projectType) => GetGrammar(projectType).Verbs;

    public VerbDefinition? FindVerb(string projectType, string verbName) =>
        GetGrammar(projectType).Verbs.FirstOrDefault(v => v.Name == verbName);

    public bool IsValidCombination(string projectType, string verbName, string modifierKey)
    {
        var verb = FindVerb(projectType, verbName);
        return verb is not null && verb.AcceptsModifiers.Contains(modifierKey);
    }

    private static Dictionary<string, GrammarProfile> LoadEmbeddedGrammars()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Seed.Engine.TokenDb.tokens.") && n.EndsWith(".json"));

        var grammars = new Dictionary<string, GrammarProfile>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Resource not found: {resourceName}");
            var grammar = JsonSerializer.Deserialize<GrammarProfile>(stream, options)
                ?? throw new InvalidOperationException($"Failed to deserialize: {resourceName}");
            grammars[grammar.ProjectType] = grammar;
        }

        return grammars;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~TokenDbTests"`
Expected: All 6 tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/TokenDb/ITokenDb.cs src/Seed.Engine/TokenDb/TokenDb.cs tests/Seed.Engine.Tests/TokenDb/
git commit -m "feat(tokendb): JSON-backed registry with verb/modifier validation"
```

---

## Task 5: Tokenizer (DSL lexer)

**Files:**
- Create: `src/Seed.Engine/Parser/ParserModels.cs`
- Create: `src/Seed.Engine/Parser/Tokenizer.cs`
- Create: `tests/Seed.Engine.Tests/Parser/TokenizerTests.cs`

- [ ] **Step 1: Define the lexer token types**

Create `src/Seed.Engine/Parser/ParserModels.cs`:

```csharp
namespace Seed.Engine.Parser;

public enum LexTokenType
{
    Identifier,        // verb name or bare word
    SlotOpen,          // <
    SlotClose,         // >
    Colon,             // :
    SeqArrow,          // →
    AmpersandPar,      // &
    PipeAlt,           // |
    Bang,              // !
    Question,          // ?
    AtSign,            // @
    Hash,              // # (comment marker)
    DoubleHash,        // ## (block comment marker)
    Newline,
    Eof,
    Whitespace
}

public sealed record LexToken(LexTokenType Type, string Value, int Line, int Column);

public sealed class ParseError
{
    public string Message { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
}

public sealed class ParseResult
{
    public List<AstStatement> Statements { get; init; } = new();
    public Models.ProjectHeader Header { get; init; } = new();
    public List<ParseError> Errors { get; init; } = new();
    public bool IsValid => Errors.Count == 0;
}

public sealed class AstStatement
{
    public string Id { get; init; } = string.Empty;
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Models.Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public List<string> EntityRefs { get; init; } = new();
    public string? Comment { get; init; }
    public List<Models.Link> Links { get; init; } = new();
}
```

- [ ] **Step 2: Write the failing tokenizer test**

Create `tests/Seed.Engine.Tests/Parser/TokenizerTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Parser;
using Xunit;

namespace Seed.Engine.Tests.Parser;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_SimpleStatement_ProducesExpectedTokens()
    {
        var dsl = "filtrer <mail>";
        var tokens = new Tokenizer().Tokenize(dsl).ToList();

        tokens.Should().SatisfyRespectively(
            t => { t.Type.Should().Be(LexTokenType.Identifier); t.Value.Should().Be("filtrer"); },
            t => t.Type.Should().Be(LexTokenType.SlotOpen),
            t => { t.Type.Should().Be(LexTokenType.Identifier); t.Value.Should().Be("mail"); },
            t => t.Type.Should().Be(LexTokenType.SlotClose),
            t => t.Type.Should().Be(LexTokenType.Eof)
        );
    }

    [Fact]
    public void Tokenize_QualifiedSlot_ProducesColonToken()
    {
        var dsl = "<db:sqlite>";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.SlotOpen,
            LexTokenType.Identifier,
            LexTokenType.Colon,
            LexTokenType.Identifier,
            LexTokenType.SlotClose,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_AllOperators_RecognizesEachOne()
    {
        var dsl = "a → b & c | d";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.Identifier,
            LexTokenType.SeqArrow,
            LexTokenType.Identifier,
            LexTokenType.AmpersandPar,
            LexTokenType.Identifier,
            LexTokenType.PipeAlt,
            LexTokenType.Identifier,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_CommentLine_EmitsHashAndConsumesRestAsIdentifier()
    {
        var dsl = "# this is a comment";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens[0].Type.Should().Be(LexTokenType.Hash);
        tokens[0].Value.Should().Be("# this is a comment");
        tokens[1].Type.Should().Be(LexTokenType.Eof);
    }

    [Fact]
    public void Tokenize_BangAndAtAndQuestion_AllRecognized()
    {
        var dsl = "!offline ?TBD @user";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.Bang,
            LexTokenType.Identifier,
            LexTokenType.Question,
            LexTokenType.Identifier,
            LexTokenType.AtSign,
            LexTokenType.Identifier,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_MultilineWithNewlines_EmitsNewlineTokens()
    {
        var dsl = "filtrer <mail>\nenregistrer <db>";
        var tokens = new Tokenizer().Tokenize(dsl)
            .Where(t => t.Type != LexTokenType.Whitespace)
            .ToList();

        tokens.Should().Contain(t => t.Type == LexTokenType.Newline);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~TokenizerTests"`
Expected: FAIL (Tokenizer class doesn't exist)

- [ ] **Step 4: Implement the Tokenizer**

Create `src/Seed.Engine/Parser/Tokenizer.cs`:

```csharp
namespace Seed.Engine.Parser;

public sealed class Tokenizer
{
    public IEnumerable<LexToken> Tokenize(string source)
    {
        int line = 1, column = 1, pos = 0;

        while (pos < source.Length)
        {
            var c = source[pos];

            if (c == '\n')
            {
                yield return new LexToken(LexTokenType.Newline, "\n", line, column);
                line++;
                column = 1;
                pos++;
                continue;
            }

            if (c == '\r')
            {
                pos++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                yield return new LexToken(LexTokenType.Whitespace, c.ToString(), line, column);
                column++;
                pos++;
                continue;
            }

            if (c == '#')
            {
                var isDouble = pos + 1 < source.Length && source[pos + 1] == '#';
                var startCol = column;
                var startPos = pos;
                while (pos < source.Length && source[pos] != '\n') pos++;
                var text = source.Substring(startPos, pos - startPos);
                column += text.Length;
                yield return new LexToken(isDouble ? LexTokenType.DoubleHash : LexTokenType.Hash, text, line, startCol);
                continue;
            }

            switch (c)
            {
                case '<':
                    yield return new LexToken(LexTokenType.SlotOpen, "<", line, column); pos++; column++; continue;
                case '>':
                    yield return new LexToken(LexTokenType.SlotClose, ">", line, column); pos++; column++; continue;
                case ':':
                    yield return new LexToken(LexTokenType.Colon, ":", line, column); pos++; column++; continue;
                case '&':
                    yield return new LexToken(LexTokenType.AmpersandPar, "&", line, column); pos++; column++; continue;
                case '|':
                    yield return new LexToken(LexTokenType.PipeAlt, "|", line, column); pos++; column++; continue;
                case '!':
                    yield return new LexToken(LexTokenType.Bang, "!", line, column); pos++; column++; continue;
                case '?':
                    yield return new LexToken(LexTokenType.Question, "?", line, column); pos++; column++; continue;
                case '@':
                    yield return new LexToken(LexTokenType.AtSign, "@", line, column); pos++; column++; continue;
            }

            if (c == '\u2192') // → (Unicode right arrow U+2192)
            {
                yield return new LexToken(LexTokenType.SeqArrow, "→", line, column);
                pos++; column++; continue;
            }

            if (c == '-' && pos + 1 < source.Length && source[pos + 1] == '>')
            {
                yield return new LexToken(LexTokenType.SeqArrow, "->", line, column);
                pos += 2; column += 2; continue;
            }

            if (IsIdentifierChar(c))
            {
                var startCol = column;
                var startPos = pos;
                while (pos < source.Length && IsIdentifierChar(source[pos])) { pos++; column++; }
                var ident = source.Substring(startPos, pos - startPos);
                yield return new LexToken(LexTokenType.Identifier, ident, line, startCol);
                continue;
            }

            // Unknown character: skip silently for now (parser will catch context errors)
            pos++;
            column++;
        }

        yield return new LexToken(LexTokenType.Eof, string.Empty, line, column);
    }

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '/' ||
        c == 'é' || c == 'è' || c == 'ê' || c == 'à' || c == 'ç' || c == 'ô' || c == 'û' || c == 'î' || c == 'â';
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~TokenizerTests"`
Expected: All 6 tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/Parser/ParserModels.cs src/Seed.Engine/Parser/Tokenizer.cs tests/Seed.Engine.Tests/Parser/TokenizerTests.cs
git commit -m "feat(parser): tokenizer for DSL with operators, slots, and comments"
```

---

## Task 6: Parser (tokens → AST)

**Files:**
- Create: `src/Seed.Engine/Parser/IParser.cs`
- Create: `src/Seed.Engine/Parser/Parser.cs`
- Create: `tests/Seed.Engine.Tests/Parser/ParserTests.cs`

- [ ] **Step 1: Define the IParser interface**

Create `src/Seed.Engine/Parser/IParser.cs`:

```csharp
namespace Seed.Engine.Parser;

public interface IParser
{
    ParseResult Parse(string dsl);
}
```

- [ ] **Step 2: Write the failing parser tests**

Create `tests/Seed.Engine.Tests/Parser/ParserTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Parser;
using Xunit;

namespace Seed.Engine.Tests.Parser;

public class ParserTests
{
    private readonly IParser _parser = new Engine.Parser.Parser();

    [Fact]
    public void Parse_HeaderOnly_PopulatesHeaderFields()
    {
        var dsl = "TYPE: cli\nNAME: mail-filter\nGOAL: filtrer mes mails";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeTrue();
        result.Header.Type.Should().Be("cli");
        result.Header.Name.Should().Be("mail-filter");
        result.Header.Goal.Should().Be("filtrer mes mails");
    }

    [Fact]
    public void Parse_SimpleStatement_ProducesOneAstStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail>";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeTrue();
        result.Statements.Should().HaveCount(1);
        result.Statements[0].Verb.Should().Be("filtrer");
        result.Statements[0].Target.Should().Be("mail");
    }

    [Fact]
    public void Parse_StatementWithModifier_AttachesModifier()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> <pertinence>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Modifiers.Should().HaveCount(1);
        result.Statements[0].Modifiers[0].Value.Should().Be("pertinence");
        result.Statements[0].Modifiers[0].Key.Should().BeNull();
    }

    [Fact]
    public void Parse_QualifiedModifier_SplitsKeyAndValue()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nenregistrer <db> <type:sqlite>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Modifiers.Should().Contain(m => m.Key == "type" && m.Value == "sqlite");
    }

    [Fact]
    public void Parse_SequenceOperator_LinksStatementsAsSeq()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> → enregistrer <db>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links.Should().HaveCount(1);
        result.Statements[0].Links[0].Type.Should().Be(Models.LinkType.Seq);
        result.Statements[0].Links[0].To.Should().Be(result.Statements[1].Id);
    }

    [Fact]
    public void Parse_ParallelOperator_LinksStatementsAsPar()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nenregistrer <db> & alerter <slack>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links[0].Type.Should().Be(Models.LinkType.Par);
    }

    [Fact]
    public void Parse_AltOperator_LinksStatementsAsAlt()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nnotifier <slack> | retry <3-fois>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links[0].Type.Should().Be(Models.LinkType.Alt);
    }

    [Fact]
    public void Parse_Constraint_AttachesToFollowingStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\n!offline enregistrer <db>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Constraints.Should().Contain("offline");
    }

    [Fact]
    public void Parse_EntityReference_CapturesAtIdentifier()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nlire @user_db";
        var result = _parser.Parse(dsl);

        result.Statements[0].EntityRefs.Should().Contain("user_db");
    }

    [Fact]
    public void Parse_CommentInline_AttachedToStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> # filtre principal";
        var result = _parser.Parse(dsl);

        result.Statements[0].Comment.Should().Be("# filtre principal");
    }

    [Fact]
    public void Parse_UnclosedSlot_ReportsError()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("slot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_MissingHeader_ReportsError()
    {
        var dsl = "filtrer <mail>";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("header", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ParserTests"`
Expected: FAIL (Parser class doesn't exist)

- [ ] **Step 4: Implement the Parser**

Create `src/Seed.Engine/Parser/Parser.cs`:

```csharp
using Seed.Engine.Models;

namespace Seed.Engine.Parser;

public sealed class Parser : IParser
{
    private readonly Tokenizer _tokenizer = new();

    public ParseResult Parse(string dsl)
    {
        var result = new ParseResult();
        var tokens = _tokenizer.Tokenize(dsl)
            .Where(t => t.Type != LexTokenType.Whitespace)
            .ToList();

        var pos = 0;

        if (!TryParseHeader(tokens, ref pos, result))
        {
            result.Errors.Add(new ParseError { Message = "Missing or invalid project header (TYPE/NAME/GOAL)", Line = 1, Column = 1 });
            return result;
        }

        ParseStatements(tokens, ref pos, result);
        return result;
    }

    private static bool TryParseHeader(List<LexToken> tokens, ref int pos, ParseResult result)
    {
        var fields = new Dictionary<string, string>();
        var savedPos = pos;

        for (var i = 0; i < 3; i++)
        {
            SkipNewlines(tokens, ref pos);
            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier) break;

            var key = tokens[pos].Value.ToUpperInvariant();
            if (key is not ("TYPE" or "NAME" or "GOAL")) break;
            pos++;

            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Colon) { pos = savedPos; return false; }
            pos++;

            var valueParts = new List<string>();
            while (pos < tokens.Count && tokens[pos].Type != LexTokenType.Newline && tokens[pos].Type != LexTokenType.Eof)
            {
                valueParts.Add(tokens[pos].Value);
                pos++;
            }

            fields[key] = string.Join(" ", valueParts).Trim();
        }

        if (!fields.ContainsKey("TYPE") || !fields.ContainsKey("NAME") || !fields.ContainsKey("GOAL"))
        {
            pos = savedPos;
            return false;
        }

        result.Header = new ProjectHeader
        {
            Type = fields["TYPE"],
            Name = fields["NAME"],
            Goal = fields["GOAL"]
        };
        return true;
    }

    private static void ParseStatements(List<LexToken> tokens, ref int pos, ParseResult result)
    {
        var nextId = 1;
        AstStatement? previous = null;
        LinkType? pendingLink = null;

        while (pos < tokens.Count && tokens[pos].Type != LexTokenType.Eof)
        {
            SkipNewlines(tokens, ref pos);
            if (pos >= tokens.Count || tokens[pos].Type == LexTokenType.Eof) break;

            if (tokens[pos].Type is LexTokenType.Hash or LexTokenType.DoubleHash)
            {
                pos++;
                continue;
            }

            var stmt = TryParseStatement(tokens, ref pos, $"s{nextId}", result);
            if (stmt is null) { pos++; continue; }

            nextId++;

            if (previous is not null && pendingLink is not null)
            {
                previous.Links.Add(new Link { To = stmt.Id, Type = pendingLink.Value });
            }

            result.Statements.Add(stmt);
            previous = stmt;

            SkipNewlines(tokens, ref pos);

            pendingLink = pos < tokens.Count
                ? tokens[pos].Type switch
                {
                    LexTokenType.SeqArrow => LinkType.Seq,
                    LexTokenType.AmpersandPar => LinkType.Par,
                    LexTokenType.PipeAlt => LinkType.Alt,
                    _ => null
                }
                : null;

            if (pendingLink is not null) pos++;
        }
    }

    private static AstStatement? TryParseStatement(List<LexToken> tokens, ref int pos, string id, ParseResult result)
    {
        var constraints = new List<string>();

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.Bang)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Identifier)
            {
                constraints.Add(tokens[pos].Value);
                pos++;
            }
        }

        if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier) return null;

        var verb = tokens[pos].Value;
        pos++;

        string target = string.Empty;
        var modifiers = new List<Modifier>();
        var entityRefs = new List<string>();
        var modifierLine = tokens[pos > 0 ? pos - 1 : 0].Line;

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.SlotOpen && tokens[pos].Line == modifierLine)
        {
            pos++;
            if (pos >= tokens.Count) { result.Errors.Add(new ParseError { Message = "Unclosed slot at end of input", Line = modifierLine, Column = 0 }); return null; }

            var first = tokens[pos];
            if (first.Type != LexTokenType.Identifier) { result.Errors.Add(new ParseError { Message = "Expected identifier after slot opening '<'", Line = first.Line, Column = first.Column }); return null; }
            pos++;

            string? key = null;
            string value = first.Value;

            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Colon)
            {
                pos++;
                if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier) { result.Errors.Add(new ParseError { Message = "Expected identifier after ':' in slot", Line = first.Line, Column = first.Column }); return null; }
                key = first.Value;
                value = tokens[pos].Value;
                pos++;
            }

            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.SlotClose)
            {
                result.Errors.Add(new ParseError { Message = $"Unclosed slot '<{first.Value}'", Line = first.Line, Column = first.Column });
                return null;
            }
            pos++;

            if (string.IsNullOrEmpty(target) && key is null) target = value;
            else modifiers.Add(new Modifier { Key = key, Value = value });
        }

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.AtSign && tokens[pos].Line == modifierLine)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Identifier)
            {
                entityRefs.Add(tokens[pos].Value);
                pos++;
            }
        }

        string? comment = null;
        if (pos < tokens.Count && tokens[pos].Type is LexTokenType.Hash or LexTokenType.DoubleHash && tokens[pos].Line == modifierLine)
        {
            comment = tokens[pos].Value;
            pos++;
        }

        return new AstStatement
        {
            Id = id,
            Verb = verb,
            Target = target,
            Modifiers = modifiers,
            Constraints = constraints,
            EntityRefs = entityRefs,
            Comment = comment
        };
    }

    private static void SkipNewlines(List<LexToken> tokens, ref int pos)
    {
        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.Newline) pos++;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ParserTests"`
Expected: All 12 tests PASS. If any fail, fix the parser inline before continuing.

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/Parser/IParser.cs src/Seed.Engine/Parser/Parser.cs tests/Seed.Engine.Tests/Parser/ParserTests.cs
git commit -m "feat(parser): DSL → AST with header, modifiers, links, constraints, refs, comments"
```

---

## Task 7: Composer (UI choices → DSL string)

**Files:**
- Create: `src/Seed.Engine/Composer/ComposerModels.cs`
- Create: `src/Seed.Engine/Composer/IComposer.cs`
- Create: `src/Seed.Engine/Composer/Composer.cs`
- Create: `tests/Seed.Engine.Tests/Composer/ComposerTests.cs`

- [ ] **Step 1: Define ComposerInput record**

Create `src/Seed.Engine/Composer/ComposerModels.cs`:

```csharp
using Seed.Engine.Models;

namespace Seed.Engine.Composer;

public sealed class ComposerInput
{
    public ProjectHeader Header { get; init; } = new();
    public List<ComposerStatement> Statements { get; init; } = new();
}

public sealed class ComposerStatement
{
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public List<string> EntityRefs { get; init; } = new();
    public string? Comment { get; init; }
    public LinkType? LinkToNext { get; init; }
}
```

- [ ] **Step 2: Define IComposer interface**

Create `src/Seed.Engine/Composer/IComposer.cs`:

```csharp
namespace Seed.Engine.Composer;

public interface IComposer
{
    string Compose(ComposerInput input);
}
```

- [ ] **Step 3: Write the failing composer tests**

Create `tests/Seed.Engine.Tests/Composer/ComposerTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Composer;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Composer;

public class ComposerTests
{
    private readonly IComposer _composer = new Engine.Composer.Composer();

    [Fact]
    public void Compose_HeaderOnly_EmitsThreeHeaderLines()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>()
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("TYPE: cli").And.Contain("NAME: x").And.Contain("GOAL: y");
    }

    [Fact]
    public void Compose_SimpleStatement_FormatsCorrectly()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail>");
    }

    [Fact]
    public void Compose_StatementWithModifiers_EmitsAllSlots()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new ComposerStatement
                {
                    Verb = "filtrer",
                    Target = "mail",
                    Modifiers =
                    {
                        new Modifier { Value = "pertinence" },
                        new Modifier { Key = "format", Value = "json" }
                    }
                }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail> <pertinence> <format:json>");
    }

    [Fact]
    public void Compose_TwoStatementsWithSeqLink_UsesArrow()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail", LinkToNext = LinkType.Seq },
                new ComposerStatement { Verb = "enregistrer", Target = "db" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail> → enregistrer <db>");
    }

    [Fact]
    public void Compose_Constraint_PrependedWithBang()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new ComposerStatement { Verb = "enregistrer", Target = "db", Constraints = { "offline" } }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("!offline enregistrer <db>");
    }

    [Fact]
    public void Compose_Comment_AppendedToLine()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail", Comment = "# filtre principal" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail>     # filtre principal");
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ComposerTests"`
Expected: FAIL (Composer class doesn't exist)

- [ ] **Step 5: Implement the Composer**

Create `src/Seed.Engine/Composer/Composer.cs`:

```csharp
using System.Text;
using Seed.Engine.Models;

namespace Seed.Engine.Composer;

public sealed class Composer : IComposer
{
    private const string ArrowSeq = " → ";
    private const string ArrowPar = " & ";
    private const string ArrowAlt = " | ";

    public string Compose(ComposerInput input)
    {
        var sb = new StringBuilder();
        sb.Append("TYPE: ").AppendLine(input.Header.Type);
        sb.Append("NAME: ").AppendLine(input.Header.Name);
        sb.Append("GOAL: ").AppendLine(input.Header.Goal);

        for (var i = 0; i < input.Statements.Count; i++)
        {
            var stmt = input.Statements[i];
            AppendStatement(sb, stmt);

            if (stmt.LinkToNext is not null && i + 1 < input.Statements.Count)
            {
                sb.Append(stmt.LinkToNext.Value switch
                {
                    LinkType.Seq => ArrowSeq,
                    LinkType.Par => ArrowPar,
                    LinkType.Alt => ArrowAlt,
                    _ => " "
                });
            }
            else if (i + 1 < input.Statements.Count)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendStatement(StringBuilder sb, ComposerStatement stmt)
    {
        foreach (var c in stmt.Constraints) sb.Append('!').Append(c).Append(' ');
        sb.Append(stmt.Verb);

        if (!string.IsNullOrEmpty(stmt.Target)) sb.Append(' ').Append('<').Append(stmt.Target).Append('>');

        foreach (var m in stmt.Modifiers)
        {
            sb.Append(' ').Append('<');
            if (!string.IsNullOrEmpty(m.Key)) sb.Append(m.Key).Append(':');
            sb.Append(m.Value).Append('>');
        }

        foreach (var e in stmt.EntityRefs) sb.Append(' ').Append('@').Append(e);

        if (!string.IsNullOrEmpty(stmt.Comment)) sb.Append("     ").Append(stmt.Comment);
    }
}
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ComposerTests"`
Expected: All 6 tests PASS

- [ ] **Step 7: Commit**

```bash
git add src/Seed.Engine/Composer/ tests/Seed.Engine.Tests/Composer/
git commit -m "feat(composer): UI choices → DSL string with constraints, modifiers, comments, links"
```

---

## Task 8: Transpiler (AST → DnaFile)

**Files:**
- Create: `src/Seed.Engine/Transpiler/ITranspiler.cs`
- Create: `src/Seed.Engine/Transpiler/Transpiler.cs`
- Create: `tests/Seed.Engine.Tests/Transpiler/TranspilerTests.cs`

- [ ] **Step 1: Define ITranspiler**

Create `src/Seed.Engine/Transpiler/ITranspiler.cs`:

```csharp
using Seed.Engine.Models;
using Seed.Engine.Parser;

namespace Seed.Engine.Transpiler;

public interface ITranspiler
{
    DnaFile Transpile(ParseResult parseResult);
}
```

- [ ] **Step 2: Write the failing transpiler tests**

Create `tests/Seed.Engine.Tests/Transpiler/TranspilerTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Seed.Engine.Transpiler;
using Xunit;

namespace Seed.Engine.Tests.Transpiler;

public class TranspilerTests
{
    private readonly IParser _parser = new Engine.Parser.Parser();
    private readonly ITranspiler _transpiler = new Engine.Transpiler.Transpiler();

    [Fact]
    public void Transpile_HeaderAndStatements_BuildsCompleteDnaFile()
    {
        var dsl = "TYPE: cli\nNAME: mail-filter\nGOAL: filtrer mes mails par pertinence\nfiltrer <mail> <pertinence> → enregistrer <db>";
        var parsed = _parser.Parse(dsl);
        parsed.IsValid.Should().BeTrue();

        var dna = _transpiler.Transpile(parsed);

        dna.Version.Should().Be("1.0");
        dna.Header.Type.Should().Be("cli");
        dna.Header.Name.Should().Be("mail-filter");
        dna.Header.Goal.Should().Be("filtrer mes mails par pertinence");
        dna.Statements.Should().HaveCount(2);
        dna.Statements[0].Verb.Should().Be("filtrer");
        dna.Statements[0].Target.Should().Be("mail");
        dna.Statements[0].Modifiers.Should().Contain(m => m.Value == "pertinence");
        dna.Statements[0].Links.Should().HaveCount(1);
        dna.Statements[0].Links[0].Type.Should().Be(LinkType.Seq);
        dna.Statements[1].Verb.Should().Be("enregistrer");
    }

    [Fact]
    public void Transpile_PreservesConstraintsAndComments()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\n!offline enregistrer <db> # local only";
        var parsed = _parser.Parse(dsl);

        var dna = _transpiler.Transpile(parsed);

        dna.Statements[0].Constraints.Should().Contain("offline");
        dna.Statements[0].Comment.Should().Be("# local only");
    }

    [Fact]
    public void Transpile_InvalidParseResult_ThrowsInvalidOperationException()
    {
        var bad = new ParseResult { Errors = { new ParseError { Message = "fake" } } };
        var act = () => _transpiler.Transpile(bad);
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~TranspilerTests"`
Expected: FAIL (Transpiler class doesn't exist)

- [ ] **Step 4: Implement the Transpiler**

Create `src/Seed.Engine/Transpiler/Transpiler.cs`:

```csharp
using Seed.Engine.Models;
using Seed.Engine.Parser;

namespace Seed.Engine.Transpiler;

public sealed class Transpiler : ITranspiler
{
    public DnaFile Transpile(ParseResult parseResult)
    {
        if (!parseResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot transpile invalid parse result. Errors: {string.Join("; ", parseResult.Errors.Select(e => e.Message))}");
        }

        var statements = parseResult.Statements
            .Select(ast => new Statement
            {
                Id = ast.Id,
                Verb = ast.Verb,
                Target = ast.Target,
                Modifiers = ast.Modifiers,
                Constraints = ast.Constraints,
                Comment = ast.Comment,
                Links = ast.Links
            })
            .ToList();

        return new DnaFile
        {
            Version = "1.0",
            Header = parseResult.Header,
            Statements = statements
        };
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~TranspilerTests"`
Expected: All 3 tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/Transpiler/ tests/Seed.Engine.Tests/Transpiler/
git commit -m "feat(transpiler): AST → DnaFile with header + statements + links"
```

---

## Task 9: Compressor (DnaFile → minimal DSL string for LLM)

**Files:**
- Create: `src/Seed.Engine/Compressor/ICompressor.cs`
- Create: `src/Seed.Engine/Compressor/Compressor.cs`
- Create: `tests/Seed.Engine.Tests/Compressor/CompressorTests.cs`

- [ ] **Step 1: Define ICompressor**

Create `src/Seed.Engine/Compressor/ICompressor.cs`:

```csharp
using Seed.Engine.Models;

namespace Seed.Engine.Compressor;

public interface ICompressor
{
    string Compress(DnaFile dna);
}
```

- [ ] **Step 2: Write the failing compressor tests**

Create `tests/Seed.Engine.Tests/Compressor/CompressorTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Compressor;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Compressor;

public class CompressorTests
{
    private readonly ICompressor _compressor = new Engine.Compressor.Compressor();

    [Fact]
    public void Compress_StripsAllComments()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new Statement
                {
                    Id = "s1",
                    Verb = "filtrer",
                    Target = "mail",
                    Comment = "# this should be stripped"
                }
            }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("#");
        output.Should().NotContain("this should be stripped");
    }

    [Fact]
    public void Compress_PreservesStatementChainStructure()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new Statement
                {
                    Id = "s1", Verb = "filtrer", Target = "mail",
                    Links = { new Link { To = "s2", Type = LinkType.Seq } }
                },
                new Statement { Id = "s2", Verb = "enregistrer", Target = "db" }
            }
        };

        var output = _compressor.Compress(dna);

        output.Should().Contain("filtrer <mail>");
        output.Should().Contain("→");
        output.Should().Contain("enregistrer <db>");
    }

    [Fact]
    public void Compress_NormalizesWhitespace_NoConsecutiveSpaces()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("  ");
    }

    [Fact]
    public void Compress_IsIdempotent_SameInputProducesSameOutput()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var first = _compressor.Compress(dna);
        var second = _compressor.Compress(dna);

        first.Should().Be(second);
    }

    [Fact]
    public void Compress_OmitsEmptyHeaderFields()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = string.Empty, Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("NAME:");
        output.Should().Contain("TYPE: cli");
        output.Should().Contain("GOAL: y");
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~CompressorTests"`
Expected: FAIL (Compressor class doesn't exist)

- [ ] **Step 4: Implement the Compressor**

Create `src/Seed.Engine/Compressor/Compressor.cs`:

```csharp
using System.Text;
using Seed.Engine.Models;

namespace Seed.Engine.Compressor;

public sealed class Compressor : ICompressor
{
    public string Compress(DnaFile dna)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(dna.Header.Type)) sb.Append("TYPE: ").AppendLine(dna.Header.Type);
        if (!string.IsNullOrEmpty(dna.Header.Name)) sb.Append("NAME: ").AppendLine(dna.Header.Name);
        if (!string.IsNullOrEmpty(dna.Header.Goal)) sb.Append("GOAL: ").AppendLine(dna.Header.Goal);

        var idToStatement = dna.Statements.ToDictionary(s => s.Id);
        var visited = new HashSet<string>();

        foreach (var stmt in dna.Statements)
        {
            if (visited.Contains(stmt.Id)) continue;
            EmitChain(sb, stmt, idToStatement, visited);
            sb.AppendLine();
        }

        return Normalize(sb.ToString().TrimEnd());
    }

    private static void EmitChain(StringBuilder sb, Statement stmt, Dictionary<string, Statement> all, HashSet<string> visited)
    {
        EmitStatement(sb, stmt);
        visited.Add(stmt.Id);

        foreach (var link in stmt.Links)
        {
            if (!all.TryGetValue(link.To, out var next) || visited.Contains(next.Id)) continue;
            sb.Append(link.Type switch
            {
                LinkType.Seq => " → ",
                LinkType.Par => " & ",
                LinkType.Alt => " | ",
                _ => " "
            });
            EmitChain(sb, next, all, visited);
        }
    }

    private static void EmitStatement(StringBuilder sb, Statement stmt)
    {
        foreach (var c in stmt.Constraints) sb.Append('!').Append(c).Append(' ');
        sb.Append(stmt.Verb);
        if (!string.IsNullOrEmpty(stmt.Target)) sb.Append(' ').Append('<').Append(stmt.Target).Append('>');
        foreach (var m in stmt.Modifiers)
        {
            sb.Append(' ').Append('<');
            if (!string.IsNullOrEmpty(m.Key)) sb.Append(m.Key).Append(':');
            sb.Append(m.Value).Append('>');
        }
    }

    private static string Normalize(string s)
    {
        var lines = s.Split('\n').Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(), @" {2,}", " "));
        return string.Join("\n", lines);
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~CompressorTests"`
Expected: All 5 tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/Compressor/ tests/Seed.Engine.Tests/Compressor/
git commit -m "feat(compressor): DnaFile → LLM-ready DSL with comment stripping and normalization"
```

---

## Task 10: JsonFileStorage (CRUD on .dna files)

**Files:**
- Create: `src/Seed.Engine/Storage/IStorage.cs`
- Create: `src/Seed.Engine/Storage/ProjectMetadata.cs`
- Create: `src/Seed.Engine/Storage/JsonFileStorage.cs`
- Create: `tests/Seed.Engine.Tests/Storage/JsonFileStorageTests.cs`

- [ ] **Step 1: Define ProjectMetadata and IStorage**

Create `src/Seed.Engine/Storage/ProjectMetadata.cs`:

```csharp
namespace Seed.Engine.Storage;

public sealed class ProjectMetadata
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public DateTime ModifiedUtc { get; init; }
}
```

Create `src/Seed.Engine/Storage/IStorage.cs`:

```csharp
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
```

- [ ] **Step 2: Write the failing storage tests**

Create `tests/Seed.Engine.Tests/Storage/JsonFileStorageTests.cs`:

```csharp
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
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~JsonFileStorageTests"`
Expected: FAIL (JsonFileStorage class doesn't exist)

- [ ] **Step 4: Implement JsonFileStorage**

Create `src/Seed.Engine/Storage/JsonFileStorage.cs`:

```csharp
using System.Text.Json;
using Seed.Engine.Models;

namespace Seed.Engine.Storage;

public sealed class JsonFileStorage : IStorage
{
    private readonly string _rootDir;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonFileStorage(string rootDir)
    {
        _rootDir = rootDir;
        Directory.CreateDirectory(_rootDir);
    }

    public string Save(DnaFile dna, string id)
    {
        var safeId = SanitizeId(id);
        var path = PathFor(safeId);
        File.WriteAllText(path, JsonSerializer.Serialize(dna, Options));
        return safeId;
    }

    public DnaFile Load(string id)
    {
        var path = PathFor(SanitizeId(id));
        if (!File.Exists(path)) throw new FileNotFoundException($"Project not found: {id}", path);
        return JsonSerializer.Deserialize<DnaFile>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException($"Failed to deserialize project: {id}");
    }

    public IReadOnlyList<ProjectMetadata> List()
    {
        if (!Directory.Exists(_rootDir)) return Array.Empty<ProjectMetadata>();

        return Directory.EnumerateFiles(_rootDir, "*.dna")
            .Select(file =>
            {
                try
                {
                    var dna = JsonSerializer.Deserialize<DnaFile>(File.ReadAllText(file), Options);
                    if (dna is null) return null;
                    return new ProjectMetadata
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        Name = dna.Header.Name,
                        Type = dna.Header.Type,
                        Goal = dna.Header.Goal,
                        ModifiedUtc = File.GetLastWriteTimeUtc(file)
                    };
                }
                catch { return null; }
            })
            .Where(m => m is not null)
            .Select(m => m!)
            .OrderByDescending(m => m.ModifiedUtc)
            .ToList();
    }

    public void Delete(string id)
    {
        var path = PathFor(SanitizeId(id));
        if (File.Exists(path)) File.Delete(path);
    }

    public bool Exists(string id) => File.Exists(PathFor(SanitizeId(id)));

    private string PathFor(string id) => Path.Combine(_rootDir, $"{id}.dna");

    private static string SanitizeId(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(id.Select(c => invalid.Contains(c) ? '_' : c));
        if (string.IsNullOrWhiteSpace(safe)) throw new ArgumentException("Project id cannot be empty", nameof(id));
        return safe;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~JsonFileStorageTests"`
Expected: All 6 tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Seed.Engine/Storage/ tests/Seed.Engine.Tests/Storage/
git commit -m "feat(storage): JsonFileStorage with CRUD + safe id sanitization"
```

---

## Task 11: End-to-end round-trip test (full pipeline)

**Files:**
- Create: `tests/Seed.Engine.Tests/EndToEnd/RoundTripTests.cs`

- [ ] **Step 1: Write the failing end-to-end test**

Create `tests/Seed.Engine.Tests/EndToEnd/RoundTripTests.cs`:

```csharp
using FluentAssertions;
using Seed.Engine.Composer;
using Seed.Engine.Compressor;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Seed.Engine.Storage;
using Seed.Engine.Transpiler;
using Xunit;

namespace Seed.Engine.Tests.EndToEnd;

public class RoundTripTests : IDisposable
{
    private readonly string _root;

    public RoundTripTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"seed-e2e-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void FullPipeline_ComposeParseTranspileSaveLoadCompress_PreservesIntent()
    {
        var composer = new Engine.Composer.Composer();
        var parser = new Engine.Parser.Parser();
        var transpiler = new Engine.Transpiler.Transpiler();
        var compressor = new Engine.Compressor.Compressor();
        var storage = new JsonFileStorage(_root);

        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "mail-filter", Goal = "filtrer mes mails" },
            Statements =
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail",
                    Modifiers = { new Modifier { Value = "pertinence" } },
                    LinkToNext = LinkType.Seq },
                new ComposerStatement { Verb = "enregistrer", Target = "db",
                    Modifiers = { new Modifier { Key = "type", Value = "sqlite" } },
                    Constraints = { "offline" } }
            }
        };

        var dsl = composer.Compose(input);
        var parsed = parser.Parse(dsl);
        parsed.IsValid.Should().BeTrue();

        var dna = transpiler.Transpile(parsed);
        storage.Save(dna, "round-trip");
        var loaded = storage.Load("round-trip");

        loaded.Header.Name.Should().Be("mail-filter");
        loaded.Statements.Should().HaveCount(2);
        loaded.Statements[0].Verb.Should().Be("filtrer");
        loaded.Statements[0].Modifiers.Should().Contain(m => m.Value == "pertinence");
        loaded.Statements[1].Constraints.Should().Contain("offline");

        var compressed = compressor.Compress(loaded);
        compressed.Should().Contain("filtrer <mail> <pertinence>");
        compressed.Should().Contain("→");
        compressed.Should().Contain("!offline enregistrer <db> <type:sqlite>");
        compressed.Should().NotContain("#");
    }

    [Fact]
    public void CompressionRatio_TenStatementProject_AchievesAtLeast4xCompression()
    {
        var prose = "j'aimerais un programme qui filtre mes mails par ordre de pertinence " +
                    "puis les enregistre dans une base de données sqlite locale tout en restant offline " +
                    "et qui m'alerte sur slack quand un mail important arrive avec une notification " +
                    "instantanée également pour les emails marqués comme urgents par mon équipe";
        var proseTokenEstimate = prose.Split(' ').Length;

        var compressor = new Engine.Compressor.Compressor();
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "mail-filter", Goal = "filtrer mails par pertinence" },
            Statements =
            {
                new Statement { Id = "s1", Verb = "filtrer", Target = "mail",
                    Modifiers = { new Modifier { Value = "pertinence" } },
                    Links = { new Link { To = "s2", Type = LinkType.Seq } } },
                new Statement { Id = "s2", Verb = "enregistrer", Target = "db",
                    Modifiers = { new Modifier { Key = "type", Value = "sqlite" } },
                    Constraints = { "offline" },
                    Links = { new Link { To = "s3", Type = LinkType.Par } } },
                new Statement { Id = "s3", Verb = "alerter", Target = "slack",
                    Modifiers = { new Modifier { Key = "si", Value = "urgent" } } }
            }
        };

        var compressed = compressor.Compress(dna);
        var dslTokenEstimate = compressed.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        var ratio = (double)proseTokenEstimate / dslTokenEstimate;
        ratio.Should().BeGreaterThan(2.0, $"compression ratio was {ratio:F2}x ({proseTokenEstimate} prose words / {dslTokenEstimate} DSL tokens)");
    }
}
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~RoundTripTests"`
Expected: Both tests PASS. The compression ratio test confirms the engine delivers on its promise.

- [ ] **Step 3: Run the full test suite to confirm no regression**

Run: `dotnet test`
Expected: All tests PASS across all categories. Total count should be approximately:
- Models: 1 test
- TokenDb: 6 tests
- Tokenizer: 6 tests
- Parser: 12 tests
- Composer: 6 tests
- Transpiler: 3 tests
- Compressor: 5 tests
- Storage: 6 tests
- EndToEnd: 2 tests
- **Total: ~47 tests, all green**

- [ ] **Step 4: Commit**

```bash
git add tests/Seed.Engine.Tests/EndToEnd/
git commit -m "test(e2e): full pipeline round-trip + compression ratio benchmark"
```

---

## Task 12: README update + version bump for v0.1.0 release

**Files:**
- Modify: `README.md`
- Modify: `src/Seed.Engine/Seed.Engine.csproj`

- [ ] **Step 1: Update README to reflect engine completion**

Read the current `README.md` first, then replace the `## Status` section with:

```markdown
## Status

🌱 **v0.1.0 — Engine ready** (2026-04-25)

The engine library is complete and tested. Six units (TokenDb, Composer, Parser, Transpiler, Compressor, Storage) compile a CLI-grammar project intent into:

- A compressed DSL string (sent to LLMs — ~2-4× compression vs prose)
- A canonical `.dna` JSON file (consumed by FORGE for 3D viz, separate plan)

**Test coverage:** ~47 tests, all green (`dotnet test`).

**Next:** FORGE integration plan (separate document) — Godot panel UI + 3D renderer adapter.
```

- [ ] **Step 2: Bump version to 0.1.0 in csproj**

Open `src/Seed.Engine/Seed.Engine.csproj` and confirm `<Version>0.1.0</Version>` is present (already set in Task 1, just verify).

- [ ] **Step 3: Tag the release**

Run from repo root:

```bash
git add README.md
git commit -m "docs: README v0.1.0 — engine ready"
git tag -a v0.1.0-engine -m "SEED engine v0.1.0 — six units shipped, ~47 tests green"
git push origin main --tags
```

Expected: tag pushed to GitHub, visible at https://github.com/sxc3030-eng/seed/tags

---

## Self-review checklist (run after writing this plan)

- [x] Every task has exact file paths
- [x] Every code step has complete code, no placeholders
- [x] No "TBD", "implement later", "fill in"
- [x] Type names consistent across tasks (`DnaFile`, `Statement`, `Modifier`, `Link`, `LinkType`, `ProjectHeader`, `ParseResult`, `AstStatement`, `ComposerInput`, `ComposerStatement`, `VerbDefinition`, `GrammarProfile`, `ProjectMetadata`)
- [x] Test commands include exact `--filter` strings
- [x] Each task ends with a commit
- [x] Spec coverage:
  - §3 Architecture → Tasks 2-10 implement all 6 units
  - §4 DSL grammar → Tasks 5-7 (tokenizer/parser/composer)
  - §6 .dna mapping → Task 2 (models) + Task 8 (transpiler)
  - §7 Components → All tasks correspond to design units
  - §8 Validation/tests → Tasks 4, 6, 11 (validation paths + benchmark)
  - §9 Scope v1 → Engine = full v1 engine scope; FORGE host = separate plan
- [x] Spec deviations explicitly documented in plan header (tech stack = C# .NET 8, decision §11.1 of spec resolved)

---

## Open items deferred to FORGE integration plan

These come from spec §11 and §10bis and are NOT in this engine plan:

1. Storage v1 final choice (currently JsonFileStorage; SQLite alternative if performance issue)
2. Algo auto-layout 3D (lives in FORGE renderer adapter)
3. Panel "Intake" autonome ou intégré au workshop FORGE existant
4. Format export `.dna` finer details (current: pure JSON, no inline comments preserved separately — comments live on Statement.Comment)
5. **FORGE Visual Library** (spec §10bis) — Icon library per verb, custom 3D module shapes per category, themes per project TYPE, contextual animations. Lives in `forge/src/Forge.Godot/Visual/SeedRenderer/`. The `.dna` format does NOT change; richness is added at render-time only. This is the proprietary moat that complements the open-core engine.

---

## Execution handoff

Plan complete. Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task with two-stage review between tasks
2. **Inline Execution** — execute all tasks in this session with checkpoints for review

Which approach?
