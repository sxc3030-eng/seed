# SEED Web Standalone Implementation Plan (Palier 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a standalone web SPA that hosts the SEED engine in a browser — same composer UX as the FORGE panel but accessible at `seed.app` (or similar) without installing FORGE. Generates compressed DSL strings to copy-paste into any LLM, downloads `.dna` files, and offers a deep-link "Open in FORGE" if FORGE is installed.

**Architecture:**

- **`Seed.Web`** — Blazor Wasm SPA project, ProjectReference to `Seed.Engine` v0.1.0 (compiled to Wasm transparently by .NET 8)
- **Storage** — `IndexedDbStorage : IStorage` via Blazored.LocalStorage or direct JS interop
- **Hosting** — static files deployed to Cloudflare Pages / Vercel / Netlify (no backend required for v0.1)
- **Cloud sync (palier 2.5)** — `SupabaseStorage : IStorage` with Supabase Auth (Google/GitHub OAuth). Out of v0.1 scope.

**Tech Stack:**
- .NET 8 Blazor WebAssembly
- `Seed.Engine` v0.1.0 (compiled to Wasm via Blazor toolchain)
- Blazored.LocalStorage 4.5.0 (IndexedDB-ish wrapper)
- xUnit + bUnit for component tests

**Why Blazor Wasm and not React/TS:**
- Engine is C#. Reusing it via Blazor = zero translation drift, zero duplicate maintenance.
- Blazor Wasm bundle is heavier (~2-3 MB initial download) but cached, and our composer is data-app not animation-app — perfect Blazor territory.
- TypeScript port of engine would mean two grammars to keep in sync — anti-DRY, contradicts open-core spec.

**Out of scope (v0.1):**
- ❌ Cloud storage (Supabase) — local IndexedDB only
- ❌ Auth / multi-user
- ❌ Sharing links (`seed.app/p/xxx`)
- ❌ Real-time collab
- ❌ Premium themes / paid features

---

## File Structure

```
seed/
├── src/
│   ├── Seed.Engine/                       (already shipped v0.1.0)
│   └── Seed.Web/                          (NEW)
│       ├── Seed.Web.csproj
│       ├── Program.cs
│       ├── App.razor
│       ├── _Imports.razor
│       ├── wwwroot/
│       │   ├── index.html
│       │   ├── css/
│       │   │   └── app.css
│       │   ├── favicon.ico
│       │   └── manifest.json              (PWA-ready)
│       ├── Pages/
│       │   ├── Index.razor                (project list / new project)
│       │   ├── Composer.razor             (the composer route /project/{id})
│       │   └── About.razor
│       ├── Components/
│       │   ├── HeaderEditor.razor
│       │   ├── StatementEditor.razor
│       │   ├── StatementListController.razor
│       │   ├── PreviewView.razor
│       │   ├── ExportToolbar.razor
│       │   └── ProjectListItem.razor
│       └── Storage/
│           ├── IndexedDbStorage.cs
│           └── BlazoredStorageAdapter.cs
└── tests/
    └── Seed.Web.Tests/                    (NEW)
        ├── Seed.Web.Tests.csproj
        ├── Components/
        │   ├── HeaderEditorTests.cs
        │   └── PreviewViewTests.cs
        └── Storage/
            └── IndexedDbStorageTests.cs   (uses an in-memory mock)
```

---

## Task 1: Scaffold Blazor Wasm project

**Files:**
- Create: `src/Seed.Web/Seed.Web.csproj`
- Create: `src/Seed.Web/Program.cs`
- Create: `src/Seed.Web/App.razor`
- Create: `src/Seed.Web/_Imports.razor`
- Create: `src/Seed.Web/wwwroot/index.html`
- Create: `src/Seed.Web/wwwroot/css/app.css`
- Modify: `Seed.sln` (add new project)

- [ ] **Step 1: Generate Blazor Wasm scaffold via dotnet new**

```bash
cd D:/ComfyUI-Intel/seed
dotnet new blazorwasm -n Seed.Web -o src/Seed.Web --no-https
```

- [ ] **Step 2: Add ProjectReference to Seed.Engine**

Edit `src/Seed.Web/Seed.Web.csproj` and add inside the `<ItemGroup>` containing other PackageReferences:

```xml
<ItemGroup>
  <ProjectReference Include="..\Seed.Engine\Seed.Engine.csproj" />
  <PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
</ItemGroup>
```

- [ ] **Step 3: Add to Seed.sln**

```bash
dotnet sln Seed.sln add src/Seed.Web/Seed.Web.csproj
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Seed.Web/Seed.Web.csproj
```
Expected: Build succeeds.

- [ ] **Step 5: Run dev server smoke test**

```bash
dotnet run --project src/Seed.Web/Seed.Web.csproj
```
Open http://localhost:5xxx in browser. Default Blazor Wasm template should render. Stop with Ctrl+C.

- [ ] **Step 6: Commit**

```bash
git checkout -b web-standalone-v0.1
git add src/Seed.Web Seed.sln
git commit -m "chore(web): scaffold Seed.Web Blazor Wasm project + Seed.Engine ref"
```

---

## Task 2: Configure Blazored.LocalStorage in Program.cs

**Files:**
- Modify: `src/Seed.Web/Program.cs`

- [ ] **Step 1: Update Program.cs to register services**

Replace contents with:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Seed.Engine.Composer;
using Seed.Engine.Compressor;
using Seed.Engine.Parser;
using Seed.Engine.Storage;
using Seed.Engine.TokenDb;
using Seed.Engine.Transpiler;
using Seed.Web;
using Seed.Web.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<ITokenDb, TokenDb>();
builder.Services.AddSingleton<IComposer, Composer>();
builder.Services.AddSingleton<IParser, Parser>();
builder.Services.AddSingleton<ITranspiler, Transpiler>();
builder.Services.AddSingleton<ICompressor, Compressor>();
builder.Services.AddScoped<IStorage, IndexedDbStorage>();

await builder.Build().RunAsync();
```

- [ ] **Step 2: Build (expect failure: IndexedDbStorage doesn't exist yet)**

That's expected — Task 3 implements it.

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Web/Program.cs
git commit -m "feat(web): register Seed.Engine services + LocalStorage in Program.cs"
```

---

## Task 3: IndexedDbStorage adapter (implements IStorage via Blazored.LocalStorage)

**Files:**
- Create: `src/Seed.Web/Storage/IndexedDbStorage.cs`

- [ ] **Step 1: Implement IndexedDbStorage**

```csharp
using Blazored.LocalStorage;
using Seed.Engine.Models;
using Seed.Engine.Storage;

namespace Seed.Web.Storage;

public sealed class IndexedDbStorage : IStorage
{
    private readonly ILocalStorageService _local;
    private const string IndexKey = "seed.projects.index";
    private static string KeyFor(string id) => $"seed.project.{id}";

    public IndexedDbStorage(ILocalStorageService local)
    {
        _local = local;
    }

    public string Save(DnaFile dna, string id)
    {
        var safeId = SanitizeId(id);
        // Sync wrappers — Blazor Wasm allows .Result on JS interop in components,
        // but for cleanliness we expose a separate async API too (see SaveAsync below).
        SaveAsync(dna, safeId).GetAwaiter().GetResult();
        return safeId;
    }

    public DnaFile Load(string id) =>
        LoadAsync(SanitizeId(id)).GetAwaiter().GetResult()
        ?? throw new FileNotFoundException($"Project not found: {id}");

    public IReadOnlyList<ProjectMetadata> List() =>
        ListAsync().GetAwaiter().GetResult();

    public void Delete(string id) =>
        DeleteAsync(SanitizeId(id)).GetAwaiter().GetResult();

    public bool Exists(string id) =>
        _local.ContainKeyAsync(KeyFor(SanitizeId(id))).GetAwaiter().GetResult();

    public async Task SaveAsync(DnaFile dna, string id)
    {
        var safeId = SanitizeId(id);
        await _local.SetItemAsync(KeyFor(safeId), dna);
        var index = await _local.GetItemAsync<List<string>>(IndexKey) ?? new();
        if (!index.Contains(safeId)) { index.Add(safeId); await _local.SetItemAsync(IndexKey, index); }
    }

    public async Task<DnaFile?> LoadAsync(string id) =>
        await _local.GetItemAsync<DnaFile>(KeyFor(id));

    public async Task<List<ProjectMetadata>> ListAsync()
    {
        var index = await _local.GetItemAsync<List<string>>(IndexKey) ?? new();
        var entries = new List<ProjectMetadata>();
        foreach (var id in index)
        {
            var dna = await _local.GetItemAsync<DnaFile>(KeyFor(id));
            if (dna is null) continue;
            entries.Add(new ProjectMetadata
            {
                Id = id,
                Name = dna.Header.Name,
                Type = dna.Header.Type,
                Goal = dna.Header.Goal,
                ModifiedUtc = DateTime.UtcNow  // IndexedDB doesn't track mtime; could store in payload later
            });
        }
        return entries;
    }

    public async Task DeleteAsync(string id)
    {
        await _local.RemoveItemAsync(KeyFor(id));
        var index = await _local.GetItemAsync<List<string>>(IndexKey) ?? new();
        index.Remove(id);
        await _local.SetItemAsync(IndexKey, index);
    }

    private static string SanitizeId(string id)
    {
        var safe = string.Concat(id.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        if (string.IsNullOrWhiteSpace(safe)) throw new ArgumentException("Project id cannot be empty", nameof(id));
        return safe;
    }
}
```

- [ ] **Step 2: Build**

```bash
cd D:/ComfyUI-Intel/seed
dotnet build src/Seed.Web/Seed.Web.csproj
```
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Web/Storage/IndexedDbStorage.cs
git commit -m "feat(web): IndexedDbStorage implements IStorage via Blazored.LocalStorage"
```

---

## Task 4: HeaderEditor component (TYPE / NAME / GOAL inputs)

**Files:**
- Create: `src/Seed.Web/Components/HeaderEditor.razor`

- [ ] **Step 1: Implement HeaderEditor.razor**

```razor
@using Seed.Engine.Models

<div class="header-editor">
    <label>TYPE:
        <select @bind="_type" @bind:after="OnChanged">
            <option value="cli">cli</option>
        </select>
    </label>
    <label>NAME:
        <input type="text" @bind="_name" @bind:event="oninput" @bind:after="OnChanged"
               placeholder="my-project" />
    </label>
    <label>GOAL:
        <input type="text" @bind="_goal" @bind:event="oninput" @bind:after="OnChanged"
               placeholder="what this project does" />
    </label>
</div>

@code {
    [Parameter] public ProjectHeader Header { get; set; } = new();
    [Parameter] public EventCallback<ProjectHeader> HeaderChanged { get; set; }

    private string _type = "cli";
    private string _name = string.Empty;
    private string _goal = string.Empty;

    protected override void OnParametersSet()
    {
        _type = Header.Type;
        _name = Header.Name;
        _goal = Header.Goal;
    }

    private async Task OnChanged()
    {
        await HeaderChanged.InvokeAsync(new ProjectHeader { Type = _type, Name = _name, Goal = _goal });
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Web/Components/HeaderEditor.razor
git commit -m "feat(web): HeaderEditor component with two-way binding"
```

---

## Task 5: StatementEditor + StatementListController components

**Files:**
- Create: `src/Seed.Web/Components/StatementEditor.razor`
- Create: `src/Seed.Web/Components/StatementListController.razor`

- [ ] **Step 1: Implement StatementEditor.razor**

```razor
@using Seed.Engine.Composer
@using Seed.Engine.Models
@using Seed.Engine.TokenDb
@inject ITokenDb TokenDb

<div class="statement-row">
    <button @onclick="() => MoveUp.InvokeAsync()">↑</button>
    <button @onclick="() => MoveDown.InvokeAsync()">↓</button>

    <select @bind="_verb" @bind:after="OnChanged">
        @foreach (var v in TokenDb.GetVerbs("cli"))
        {
            <option value="@v.Name">@v.Name</option>
        }
    </select>

    <input type="text" @bind="_target" @bind:event="oninput" @bind:after="OnChanged"
           placeholder="<target>" />

    <input type="text" @bind="_modifiersRaw" @bind:event="oninput" @bind:after="OnChanged"
           placeholder="modifier1, key:value, ..." />

    <select @bind="_link" @bind:after="OnChanged">
        <option value="">(none)</option>
        <option value="seq">→ seq</option>
        <option value="par">& par</option>
        <option value="alt">| alt</option>
    </select>

    <input type="text" @bind="_comment" @bind:event="oninput" @bind:after="OnChanged"
           placeholder="# comment" />

    <button @onclick="() => Remove.InvokeAsync()">✕</button>
</div>

@code {
    [Parameter] public EventCallback<ComposerStatement> StatementChanged { get; set; }
    [Parameter] public EventCallback Remove { get; set; }
    [Parameter] public EventCallback MoveUp { get; set; }
    [Parameter] public EventCallback MoveDown { get; set; }

    private string _verb = "filtrer";
    private string _target = string.Empty;
    private string _modifiersRaw = string.Empty;
    private string _link = string.Empty;
    private string _comment = string.Empty;

    private async Task OnChanged()
    {
        var modifiers = ParseModifiers(_modifiersRaw);
        var stmt = new ComposerStatement
        {
            Verb = _verb,
            Target = _target.Trim(),
            Modifiers = modifiers,
            Comment = string.IsNullOrWhiteSpace(_comment) ? null : _comment,
            LinkToNext = _link switch
            {
                "seq" => LinkType.Seq,
                "par" => LinkType.Par,
                "alt" => LinkType.Alt,
                _ => null
            }
        };
        await StatementChanged.InvokeAsync(stmt);
    }

    private static List<Modifier> ParseModifiers(string text)
    {
        var result = new List<Modifier>();
        foreach (var raw in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = raw.Trim();
            if (trimmed.Contains(':'))
            {
                var parts = trimmed.Split(':', 2);
                result.Add(new Modifier { Key = parts[0].Trim(), Value = parts[1].Trim() });
            }
            else
            {
                result.Add(new Modifier { Value = trimmed });
            }
        }
        return result;
    }
}
```

- [ ] **Step 2: Implement StatementListController.razor**

```razor
@using Seed.Engine.Composer

<div class="statement-list">
    @for (var i = 0; i < _statements.Count; i++)
    {
        var index = i;
        <StatementEditor
            @key="index"
            StatementChanged="@(s => UpdateStatement(index, s))"
            Remove="@(() => RemoveAt(index))"
            MoveUp="@(() => Move(index, -1))"
            MoveDown="@(() => Move(index, +1))" />
    }
    <button class="add-btn" @onclick="AddStatement">+ Statement</button>
</div>

@code {
    [Parameter] public EventCallback<List<ComposerStatement>> ListChanged { get; set; }

    private readonly List<ComposerStatement> _statements = new();

    protected override void OnInitialized()
    {
        if (_statements.Count == 0) AddStatement();
    }

    private async Task AddStatement()
    {
        _statements.Add(new ComposerStatement { Verb = "filtrer" });
        await ListChanged.InvokeAsync(_statements);
    }

    private async Task UpdateStatement(int index, ComposerStatement stmt)
    {
        if (index < 0 || index >= _statements.Count) return;
        _statements[index] = stmt;
        await ListChanged.InvokeAsync(_statements);
    }

    private async Task RemoveAt(int index)
    {
        if (index < 0 || index >= _statements.Count) return;
        _statements.RemoveAt(index);
        await ListChanged.InvokeAsync(_statements);
    }

    private async Task Move(int index, int delta)
    {
        var newIndex = Math.Clamp(index + delta, 0, _statements.Count - 1);
        if (newIndex == index) return;
        var item = _statements[index];
        _statements.RemoveAt(index);
        _statements.Insert(newIndex, item);
        await ListChanged.InvokeAsync(_statements);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Web/Components/StatementEditor.razor src/Seed.Web/Components/StatementListController.razor
git commit -m "feat(web): StatementEditor + StatementListController with add/remove/reorder"
```

---

## Task 6: PreviewView component (live DSL + token counter + validation)

**Files:**
- Create: `src/Seed.Web/Components/PreviewView.razor`

- [ ] **Step 1: Implement PreviewView.razor**

```razor
@using Seed.Engine.Composer
@using Seed.Engine.Parser
@inject IComposer Composer
@inject IParser Parser

<div class="preview">
    <div class="status @(_isValid ? "valid" : "invalid")">
        <span class="dot"></span>
        @_statusText
    </div>
    <pre class="dsl-output"><code>@_dsl</code></pre>
</div>

@code {
    [Parameter] public ComposerInput Input { get; set; } = new();

    private string _dsl = string.Empty;
    private bool _isValid = true;
    private string _statusText = "no input yet";

    protected override void OnParametersSet()
    {
        _dsl = Composer.Compose(Input);
        var parsed = Parser.Parse(_dsl);
        _isValid = parsed.IsValid;
        if (_isValid)
        {
            var tokenEstimate = _dsl.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            _statusText = $"valid — ~{tokenEstimate} tokens";
        }
        else
        {
            _statusText = parsed.Errors.Count > 0
                ? $"{parsed.Errors.Count} error(s) — {parsed.Errors[0].Message}"
                : "invalid";
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Web/Components/PreviewView.razor
git commit -m "feat(web): PreviewView with live DSL preview + validation indicator"
```

---

## Task 7: ExportToolbar component (copy / download / open in FORGE)

**Files:**
- Create: `src/Seed.Web/Components/ExportToolbar.razor`

- [ ] **Step 1: Implement ExportToolbar.razor**

```razor
@using System.Text.Json
@using Microsoft.JSInterop
@using Seed.Engine.Compressor
@using Seed.Engine.Models
@inject ICompressor Compressor
@inject IJSRuntime JS

<div class="export-toolbar">
    <button @onclick="OnCopy" disabled="@(Dna is null)">📋 Copy DSL</button>
    <button @onclick="OnDownload" disabled="@(Dna is null)">💾 Download .dna</button>
    <button @onclick="OnOpenInForge" disabled="@(Dna is null)">⮕ Open in FORGE</button>
    @if (!string.IsNullOrEmpty(_status)) { <span class="status-msg">@_status</span> }
</div>

@code {
    [Parameter] public DnaFile? Dna { get; set; }
    private string _status = string.Empty;

    private async Task OnCopy()
    {
        if (Dna is null) return;
        var compressed = Compressor.Compress(Dna);
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", compressed);
        _status = $"copied {compressed.Length} chars";
    }

    private async Task OnDownload()
    {
        if (Dna is null) return;
        var json = JsonSerializer.Serialize(Dna, new JsonSerializerOptions { WriteIndented = true });
        var name = string.IsNullOrWhiteSpace(Dna.Header.Name) ? "untitled" : Dna.Header.Name;
        await JS.InvokeVoidAsync("seedDownload", $"{name}.dna", json);
        _status = $"downloaded {name}.dna";
    }

    private async Task OnOpenInForge()
    {
        if (Dna is null) return;
        var json = JsonSerializer.Serialize(Dna);
        var encoded = Uri.EscapeDataString(json);
        var url = $"forge://open-dna?payload={encoded}";
        await JS.InvokeVoidAsync("window.open", url, "_self");
        _status = "attempting to open FORGE...";
    }
}
```

- [ ] **Step 2: Add JS helper for download in wwwroot/index.html**

Add before `</body>`:

```html
<script>
window.seedDownload = (filename, content) => {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
};
</script>
```

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Web/Components/ExportToolbar.razor src/Seed.Web/wwwroot/index.html
git commit -m "feat(web): ExportToolbar with copy/download/open-in-FORGE deep-link"
```

---

## Task 8: Composer page (assembles all components)

**Files:**
- Create: `src/Seed.Web/Pages/Composer.razor`

- [ ] **Step 1: Implement Composer.razor**

```razor
@page "/project/{Id?}"
@using Seed.Engine.Composer
@using Seed.Engine.Models
@using Seed.Engine.Parser
@using Seed.Engine.Storage
@using Seed.Engine.Transpiler
@inject IParser Parser
@inject ITranspiler Transpiler
@inject IStorage Storage
@inject NavigationManager Nav

<PageTitle>SEED — @(_input.Header.Name ?? "new project")</PageTitle>

<div class="composer-page">
    <div class="topbar">
        <h1>🌱 SEED</h1>
        <button @onclick="GoHome">⬅ Projects</button>
        <button @onclick="OnSave" disabled="@(_dna is null)">💾 Save</button>
    </div>

    <HeaderEditor Header="_input.Header" HeaderChanged="OnHeaderChanged" />

    <hr />

    <StatementListController ListChanged="OnListChanged" />

    <hr />

    <PreviewView Input="_input" />

    <ExportToolbar Dna="_dna" />
</div>

@code {
    [Parameter] public string? Id { get; set; }

    private ComposerInput _input = new();
    private DnaFile? _dna;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(Id) && Storage.Exists(Id))
        {
            var loaded = Storage.Load(Id);
            _input = new ComposerInput
            {
                Header = loaded.Header,
                Statements = loaded.Statements.Select(s => new ComposerStatement
                {
                    Verb = s.Verb,
                    Target = s.Target,
                    Modifiers = s.Modifiers,
                    Constraints = s.Constraints,
                    Comment = s.Comment,
                    LinkToNext = s.Links.Count > 0 ? s.Links[0].Type : null
                }).ToList()
            };
            Recompute();
        }
        await Task.CompletedTask;
    }

    private void OnHeaderChanged(ProjectHeader h)
    {
        _input = new ComposerInput { Header = h, Statements = _input.Statements };
        Recompute();
    }

    private void OnListChanged(List<ComposerStatement> statements)
    {
        _input = new ComposerInput { Header = _input.Header, Statements = statements };
        Recompute();
    }

    private void Recompute()
    {
        var composer = new Seed.Engine.Composer.Composer();
        var dsl = composer.Compose(_input);
        var parsed = Parser.Parse(dsl);
        _dna = parsed.IsValid ? Transpiler.Transpile(parsed) : null;
    }

    private void OnSave()
    {
        if (_dna is null) return;
        var id = string.IsNullOrWhiteSpace(_input.Header.Name) ? "untitled" : _input.Header.Name;
        Storage.Save(_dna, id);
    }

    private void GoHome() => Nav.NavigateTo("/");
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Web/Pages/Composer.razor
git commit -m "feat(web): Composer page route /project/{id} with full pipeline + save"
```

---

## Task 9: Index page (project list / new project)

**Files:**
- Modify: `src/Seed.Web/Pages/Index.razor` (replace template content)
- Create: `src/Seed.Web/Components/ProjectListItem.razor`

- [ ] **Step 1: Implement ProjectListItem.razor**

```razor
@using Seed.Engine.Storage

<div class="project-card">
    <div class="title">@Metadata.Name</div>
    <div class="meta">@Metadata.Type — @Metadata.Goal</div>
    <div class="actions">
        <button @onclick="() => Open.InvokeAsync(Metadata.Id)">Open</button>
        <button @onclick="() => Delete.InvokeAsync(Metadata.Id)" class="danger">Delete</button>
    </div>
</div>

@code {
    [Parameter] public ProjectMetadata Metadata { get; set; } = new();
    [Parameter] public EventCallback<string> Open { get; set; }
    [Parameter] public EventCallback<string> Delete { get; set; }
}
```

- [ ] **Step 2: Replace Index.razor**

```razor
@page "/"
@using Seed.Engine.Storage
@inject IStorage Storage
@inject NavigationManager Nav

<PageTitle>SEED — Projects</PageTitle>

<div class="home">
    <h1>🌱 SEED Projects</h1>
    <button class="new-btn" @onclick="OnNew">+ New project</button>

    @if (_projects.Count == 0)
    {
        <p class="empty">No projects yet. Click "New project" to start.</p>
    }
    else
    {
        <div class="project-grid">
            @foreach (var p in _projects)
            {
                <ProjectListItem Metadata="p" Open="OnOpen" Delete="OnDelete" />
            }
        </div>
    }
</div>

@code {
    private List<ProjectMetadata> _projects = new();

    protected override async Task OnInitializedAsync()
    {
        _projects = Storage.List().ToList();
        await Task.CompletedTask;
    }

    private void OnNew() => Nav.NavigateTo("/project/new-" + DateTime.UtcNow.Ticks);

    private void OnOpen(string id) => Nav.NavigateTo($"/project/{id}");

    private void OnDelete(string id)
    {
        Storage.Delete(id);
        _projects = Storage.List().ToList();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Web/Components/ProjectListItem.razor src/Seed.Web/Pages/Index.razor
git commit -m "feat(web): Index project list + ProjectListItem with open/delete"
```

---

## Task 10: Minimal CSS for layout

**Files:**
- Replace: `src/Seed.Web/wwwroot/css/app.css`

- [ ] **Step 1: Write app.css**

```css
:root {
    --bg: #0f172a;
    --panel: #1e293b;
    --accent: #22c55e;
    --error: #ef4444;
    --warn: #f59e0b;
    --text: #f1f5f9;
    --muted: #64748b;
}

body { margin: 0; font-family: system-ui, -apple-system, sans-serif; background: var(--bg); color: var(--text); }
h1 { margin: 0; font-size: 1.5rem; }

.home, .composer-page { max-width: 960px; margin: 0 auto; padding: 1rem; }
.topbar { display: flex; gap: 1rem; align-items: center; margin-bottom: 1rem; }

.header-editor { display: flex; gap: 1rem; flex-wrap: wrap; padding: 0.75rem; background: var(--panel); border-radius: 6px; }
.header-editor label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.85rem; color: var(--muted); }
.header-editor input, .header-editor select { padding: 0.4rem 0.6rem; background: #0f172a; border: 1px solid #334155; color: var(--text); border-radius: 4px; }

.statement-list { display: flex; flex-direction: column; gap: 0.5rem; padding: 0.75rem; background: var(--panel); border-radius: 6px; }
.statement-row { display: flex; gap: 0.4rem; align-items: center; }
.statement-row input, .statement-row select { padding: 0.3rem 0.5rem; background: #0f172a; border: 1px solid #334155; color: var(--text); border-radius: 4px; font-family: ui-monospace, monospace; }
.add-btn { background: var(--accent); color: black; border: 0; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; }

.preview { padding: 0.75rem; background: var(--panel); border-radius: 6px; }
.preview .status { display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; margin-bottom: 0.5rem; }
.preview .dot { width: 12px; height: 12px; border-radius: 50%; background: var(--muted); }
.preview .status.valid .dot { background: var(--accent); }
.preview .status.invalid .dot { background: var(--error); }
.dsl-output { background: #0f172a; padding: 1rem; border-radius: 4px; overflow-x: auto; font-family: ui-monospace, monospace; }

.export-toolbar { display: flex; gap: 0.5rem; align-items: center; margin-top: 1rem; }
.export-toolbar button { padding: 0.5rem 1rem; background: #334155; color: var(--text); border: 0; border-radius: 4px; cursor: pointer; }
.export-toolbar button:disabled { opacity: 0.4; cursor: not-allowed; }
.status-msg { color: var(--muted); font-size: 0.85rem; }

.project-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1rem; margin-top: 1rem; }
.project-card { padding: 1rem; background: var(--panel); border-radius: 6px; }
.project-card .title { font-weight: 600; }
.project-card .meta { font-size: 0.85rem; color: var(--muted); margin: 0.5rem 0; }
.project-card .actions { display: flex; gap: 0.5rem; }
.project-card button { background: #334155; color: var(--text); border: 0; padding: 0.4rem 0.8rem; border-radius: 4px; cursor: pointer; }
.project-card button.danger { background: var(--error); }

.empty { color: var(--muted); margin-top: 2rem; text-align: center; }
.new-btn { background: var(--accent); color: black; border: 0; padding: 0.5rem 1.2rem; border-radius: 4px; cursor: pointer; font-size: 1rem; }
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Web/wwwroot/css/app.css
git commit -m "feat(web): minimal dark-mode CSS for composer + project list"
```

---

## Task 11: Smoke test in browser

**Files:** none — pure verification.

- [ ] **Step 1: Run dev server**

```bash
cd D:/ComfyUI-Intel/seed
dotnet run --project src/Seed.Web/Seed.Web.csproj
```

- [ ] **Step 2: Open http://localhost:5xxx in browser**

Verify:
- Index page shows "No projects yet"
- Click "+ New project" → routes to /project/new-XXX
- Compose: TYPE=cli, NAME=smoke-test, GOAL=verify web works
- Add 2 statements: filtrer <mail> → enregistrer <db>
- Preview shows valid DSL with token count
- Click "Save" → toast/inline confirmation
- Navigate back → smoke-test appears in project list
- Reload page (F5) → smoke-test still there (IndexedDB persistence)
- Click "Open" → loads back into composer correctly

- [ ] **Step 3: Click "Copy DSL" → paste in Notepad to verify clipboard works**

- [ ] **Step 4: Click "Download .dna" → verify file downloads**

- [ ] **Step 5: Capture screenshot for README**

Save to `docs/screenshots/seed-web-v0.1.png`

---

## Task 12: Static deployment to Cloudflare Pages (or alternative)

**Files:**
- Create: `.github/workflows/deploy-web.yml` (if using GitHub Actions)

- [ ] **Step 1: Build static publish**

```bash
cd D:/ComfyUI-Intel/seed
dotnet publish src/Seed.Web/Seed.Web.csproj -c Release -o publish/seed-web
```

Output: `publish/seed-web/wwwroot/` contains the static SPA.

- [ ] **Step 2: Choose hosting**

Options:
- **Cloudflare Pages** : free, fast, custom domain — connect repo via Cloudflare dashboard, set build command `dotnet publish src/Seed.Web -c Release -o publish/seed-web`, output dir `publish/seed-web/wwwroot`
- **Vercel** : same idea, configure via UI or vercel.json
- **Netlify** : same

For this plan, recommend Cloudflare Pages (best for Wasm: HTTP/3, brotli compression).

- [ ] **Step 3: Set up GitHub Action (optional)**

Create `.github/workflows/deploy-web.yml`:

```yaml
name: Deploy Web

on:
  push:
    branches: [main]
    paths:
      - 'src/Seed.Engine/**'
      - 'src/Seed.Web/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - run: dotnet publish src/Seed.Web/Seed.Web.csproj -c Release -o publish/seed-web
      - uses: cloudflare/pages-action@v1
        with:
          apiToken: ${{ secrets.CLOUDFLARE_API_TOKEN }}
          accountId: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
          projectName: seed-web
          directory: publish/seed-web/wwwroot
```

(Requires user to set up Cloudflare API token + account ID as repo secrets.)

- [ ] **Step 4: Commit + tag release**

```bash
git add .github/workflows/deploy-web.yml
git commit -m "chore(web): GitHub Action deploy to Cloudflare Pages"

git checkout main
git merge web-standalone-v0.1 --ff-only
git tag -a v0.1.0-web -m "SEED web standalone v0.1.0 — Blazor Wasm SPA shipped"
git push origin main --tags
```

---

## Self-Review Checklist

- [x] Each task has exact file paths
- [x] Components use proper Blazor parameter binding patterns
- [x] No placeholders / TBDs
- [x] Type names consistent (`HeaderEditor`, `StatementEditor`, `StatementListController`, `PreviewView`, `ExportToolbar`, `ProjectListItem`, `IndexedDbStorage`)
- [x] Engine reuse via ProjectReference (no rewrite)
- [x] Storage adapter implements `Seed.Engine.Storage.IStorage` cleanly
- [x] Spec coverage:
  - §3 architecture → Tasks 1, 2 (Blazor scaffold + DI registration)
  - §5 UI flow → Tasks 4-9 (header / statements / preview / export / pages)
  - §7 components → engine consumed via DI, components are 1:1 mirrors of FORGE panel parts
  - §9 v1 scope → 12 tasks ship a full functional SPA without cloud or auth (palier 2.5 deferred)

---

## Deferred to palier 2.5 (separate plan)

1. **SupabaseStorage** — cloud sync via Supabase, requires auth setup
2. **Sharing links** — `seed.app/p/<short-id>` with read-only view
3. **Multi-user / collaboration** — real-time CRDT or operation transform
4. **PWA** — service worker for offline, install prompt
5. **Mobile-friendly responsive** — current CSS is desktop-first
6. **Accessibility audit** — keyboard navigation, screen reader, ARIA

---

## Execution Handoff

Plan complete. To execute via subagent-driven development:

**Next session prompt :**

> Execute the SEED web standalone plan at `seed/docs/superpowers/plans/2026-04-25-seed-web-standalone-plan.md` using superpowers:subagent-driven-development. 12 tasks. Tasks 1, 3, 10, 11, 12 are mechanical (Haiku). Tasks 4-9 (Razor components) benefit from Sonnet to handle Blazor binding subtleties. Create branch `web-standalone-v0.1` from current main of seed/ repo.

Pre-req : Seed.Engine v0.1.0 already shipped (✅).
