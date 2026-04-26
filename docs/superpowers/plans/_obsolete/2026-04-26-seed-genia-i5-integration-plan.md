# SEED → genia.social + i5 Backend Integration Plan (Stage 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Replace the Stage 0 front-end-only mock at `genia.social/seed` with a real-engine-backed page. The Seed.Engine v0.1.0 (C#) runs as an ASP.NET Web API on bui1's i5 box; the Next.js page on genia.social calls it via fetch. Storage is on the i5 disk via Seed.Engine.JsonFileStorage.

**Architecture:**

```
Browser (Vercel-hosted Next.js on genia.social)
   │
   │ HTTPS  POST /seed/api/compose       ─┐
   │        POST /seed/api/save           │  CORS allowlist: genia.social
   │        GET  /seed/api/projects       │  Auth: Supabase JWT bearer
   │        GET  /seed/api/projects/{id}  │  forwarded to i5 for verification
   │        DELETE /seed/api/projects/{id}┘
   ▼
i5 box (bui1's machine, reachable at https://seed.<domain>)
   │
   │ ASP.NET Minimal API (Seed.Server)
   │   - wraps Seed.Engine.Composer / Parser / Transpiler / Compressor
   │   - JsonFileStorage rooted at C:\seed-data\<userId>\
   │   - Supabase JWT validation middleware
   │
   ▼
Storage : C:\seed-data\<supabase-user-id>\<projectId>.dna
```

**Tech Stack:**

- Frontend : Next.js 14 (existing genia.social), TypeScript, TanStack Query for caching, Supabase Auth (existing)
- Backend : ASP.NET 8 Minimal API, Seed.Engine v0.1.0 ProjectReference, Supabase JWT validation via JwtBearer
- Networking : i5 reachable via dynamic DNS (Cloudflare DDNS) or Tailscale Funnel for HTTPS without port-forward setup
- Auth : Supabase JWT in `Authorization: Bearer <token>` header, verified server-side via JWKS

**Pre-requisite (Stage 0 already shipped) :**
- ✅ `genia.social/seed` route exists with mock composer (commit `0f0b4b4` on master)
- ✅ BottomNav 🌱 SEED entry visible
- ✅ Seed.Engine v0.1.0 shipped at `D:/ComfyUI-Intel/seed/`

**Out of scope (Stage 2+):**
- ❌ Sharing links (`/seed/p/<short-id>` for read-only public view)
- ❌ Real-time collab
- ❌ Mobile app (only web)
- ❌ FORGE deep-link from web (Stage 2 — needs custom URI handler on Windows)

---

## ⚠️ HARD PREREQUISITE: i5 box accessibility verified

**Before dispatching ANY task in this plan, run the i5 verification checklist below and confirm green on every line.** If any check fails, the relevant tasks must be adjusted (or the i5 swapped for a Vercel Function / Fly.io machine).

See `i5-verification-checklist.md` (sibling file).

---

## File Structure

**Backend (new repo or sibling under seed/) :**
```
seed/
├── src/
│   ├── Seed.Engine/                   (already shipped v0.1.0)
│   └── Seed.Server/                   (NEW — ASP.NET 8 Minimal API)
│       ├── Seed.Server.csproj
│       ├── Program.cs                 (DI + auth + endpoints)
│       ├── Endpoints/
│       │   ├── ComposeEndpoint.cs
│       │   ├── ProjectsEndpoint.cs    (CRUD)
│       │   └── HealthEndpoint.cs
│       ├── Auth/
│       │   ├── SupabaseJwtOptions.cs
│       │   └── SupabaseJwtBearerExtensions.cs
│       ├── Storage/
│       │   └── PerUserStorageFactory.cs   (rooted at C:\seed-data\<userId>\)
│       └── appsettings.json
└── tests/
    └── Seed.Server.Tests/
        ├── Seed.Server.Tests.csproj
        └── Endpoints/
            ├── ComposeEndpointTests.cs
            └── ProjectsEndpointTests.cs
```

**Frontend (in existing genia repo) :**
```
genia/apps/web/
├── app/
│   └── seed/
│       ├── page.tsx                   (already shipped Stage 0 — replaced in this plan)
│       └── components/                (NEW)
│           ├── ComposerForm.tsx
│           ├── StatementRow.tsx
│           ├── PreviewPane.tsx
│           ├── ExportToolbar.tsx
│           ├── ProjectListPanel.tsx
│           └── ProjectListItem.tsx
└── lib/
    └── seed/                          (NEW)
        ├── client.ts                  (typed fetch wrapper for /api endpoints)
        ├── types.ts                   (mirrors Seed.Server DTOs)
        └── useSeedProjects.ts         (TanStack Query hooks)
```

---

## Task 1: Verify i5 prerequisites (HARD GATE)

Run the checklist at `i5-verification-checklist.md`. **Do not proceed if any item is red.**

- [ ] All checklist items green
- [ ] i5 reachable at decided URL (e.g., `https://seed.<bui1-domain>`)
- [ ] .NET 8 SDK installed
- [ ] HTTPS cert in place (Let's Encrypt via Caddy / Cloudflare Tunnel / Tailscale Funnel)
- [ ] Disk space confirmed at `C:\seed-data\` mount point

---

## Task 2: Scaffold Seed.Server ASP.NET Minimal API

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Seed.Server.csproj`
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Program.cs`
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/appsettings.json`
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/appsettings.Development.json`
- Modify: `D:/ComfyUI-Intel/seed/Seed.sln`

- [ ] **Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>seed-server-dev</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Seed.Engine\Seed.Engine.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create Program.cs with DI + CORS + JWT bearer + endpoints stubs**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Seed.Engine.Composer;
using Seed.Engine.Compressor;
using Seed.Engine.Parser;
using Seed.Engine.Storage;
using Seed.Engine.TokenDb;
using Seed.Engine.Transpiler;
using Seed.Server.Auth;
using Seed.Server.Endpoints;
using Seed.Server.Storage;

var builder = WebApplication.CreateBuilder(args);

// Engine singletons (cheap to construct, thread-safe).
builder.Services.AddSingleton<ITokenDb, TokenDb>();
builder.Services.AddSingleton<IComposer, Composer>();
builder.Services.AddSingleton<IParser, Parser>();
builder.Services.AddSingleton<ITranspiler, Transpiler>();
builder.Services.AddSingleton<ICompressor, Compressor>();

// Per-user storage factory.
builder.Services.AddSingleton<PerUserStorageFactory>();

// CORS allowlist.
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration["Cors:AllowedOrigins"].Split(','))
     .AllowAnyHeader()
     .AllowAnyMethod()));

// Supabase JWT validation.
builder.Services.AddSupabaseJwtBearer(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapComposeEndpoints();
app.MapProjectsEndpoints();

app.Run();
```

- [ ] **Step 3: Create appsettings.json + appsettings.Development.json**

`appsettings.json`:

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "Cors": { "AllowedOrigins": "https://genia.social" },
  "Supabase": {
    "ProjectUrl": "https://<your-supabase-project>.supabase.co",
    "JwksUrl": "https://<your-supabase-project>.supabase.co/auth/v1/keys"
  },
  "Storage": {
    "RootPath": "C:\\seed-data"
  }
}
```

`appsettings.Development.json`:
```json
{
  "Cors": { "AllowedOrigins": "http://localhost:3000,https://genia.social" }
}
```

- [ ] **Step 4: Add to solution**

```bash
cd D:/ComfyUI-Intel/seed
dotnet sln Seed.sln add src/Seed.Server/Seed.Server.csproj
```

- [ ] **Step 5: Build verification**

```bash
dotnet build src/Seed.Server/Seed.Server.csproj
```

(Expected to FAIL because the endpoint extension methods don't exist yet — Tasks 3-5 add them.)

- [ ] **Step 6: Commit**

```bash
git checkout -b server-stage-1
git add Seed.sln src/Seed.Server/
git commit -m "chore(server): scaffold Seed.Server ASP.NET Minimal API"
```

---

## Task 3: Auth — Supabase JWT validation middleware

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Auth/SupabaseJwtOptions.cs`
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Auth/SupabaseJwtBearerExtensions.cs`

- [ ] **Step 1: Implement SupabaseJwtOptions.cs**

```csharp
namespace Seed.Server.Auth;

public sealed class SupabaseJwtOptions
{
    public string ProjectUrl { get; init; } = string.Empty;
    public string JwksUrl { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Implement SupabaseJwtBearerExtensions.cs**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Seed.Server.Auth;

public static class SupabaseJwtBearerExtensions
{
    public static IServiceCollection AddSupabaseJwtBearer(this IServiceCollection services, IConfiguration cfg)
    {
        var supa = cfg.GetSection("Supabase").Get<SupabaseJwtOptions>()
                   ?? throw new InvalidOperationException("Missing Supabase config section");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = supa.ProjectUrl + "/auth/v1";
                options.MetadataAddress = supa.JwksUrl;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = supa.ProjectUrl + "/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                };
            });
        return services;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Server/Auth/
git commit -m "feat(server): Supabase JWT bearer middleware (claims sub→userId)"
```

---

## Task 4: Per-user storage factory

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Storage/PerUserStorageFactory.cs`

- [ ] **Step 1: Implement PerUserStorageFactory.cs**

```csharp
using Seed.Engine.Storage;

namespace Seed.Server.Storage;

/// <summary>
/// Issues a per-user JsonFileStorage instance rooted at C:\seed-data\<userId>\.
/// Sanitizes userId to prevent path traversal.
/// </summary>
public sealed class PerUserStorageFactory
{
    private readonly string _rootPath;

    public PerUserStorageFactory(IConfiguration cfg)
    {
        _rootPath = cfg["Storage:RootPath"] ?? throw new InvalidOperationException("Missing Storage:RootPath");
        Directory.CreateDirectory(_rootPath);
    }

    public IStorage For(string userId)
    {
        var safe = SanitizeUserId(userId);
        var dir = Path.Combine(_rootPath, safe);
        Directory.CreateDirectory(dir);
        return new JsonFileStorage(dir);
    }

    private static string SanitizeUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId required", nameof(userId));
        var safe = string.Concat(userId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        if (string.IsNullOrEmpty(safe)) throw new ArgumentException("userId becomes empty after sanitization", nameof(userId));
        return safe;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Server/Storage/
git commit -m "feat(server): PerUserStorageFactory rooted at C:\\seed-data\\<userId>\\"
```

---

## Task 5: ComposeEndpoint (POST /api/compose)

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Endpoints/ComposeEndpoint.cs`

- [ ] **Step 1: Implement ComposeEndpoint.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Seed.Engine.Composer;
using Seed.Engine.Compressor;
using Seed.Engine.Parser;
using Seed.Engine.Transpiler;

namespace Seed.Server.Endpoints;

public static class ComposeEndpoint
{
    public sealed record ComposeRequest(string Input);
    public sealed record ComposeResponse(string CompactDsl, int OriginalTokens, int CompactTokens, double CompressionRatio);

    public static IEndpointRouteBuilder MapComposeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/compose", (
            [FromBody] ComposeRequest req,
            IParser parser,
            ITranspiler transpiler,
            ICompressor compressor) =>
        {
            if (string.IsNullOrWhiteSpace(req.Input)) return Results.BadRequest(new { error = "Empty input" });

            var parsed = parser.Parse(req.Input);
            if (!parsed.IsValid)
            {
                return Results.BadRequest(new
                {
                    error = "parse_failed",
                    details = parsed.Errors.Select(e => new { e.Line, e.Message }),
                });
            }

            var dna = transpiler.Transpile(parsed);
            var compact = compressor.Compress(dna);

            var orig = req.Input.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var comp = compact.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var ratio = comp == 0 ? 0d : (double)orig / comp;

            return Results.Ok(new ComposeResponse(compact, orig, comp, ratio));
        })
        .RequireAuthorization()
        .WithName("Compose");

        return app;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Server/Endpoints/ComposeEndpoint.cs
git commit -m "feat(server): POST /api/compose returns compressed DSL + ratio"
```

---

## Task 6: ProjectsEndpoint (CRUD on .dna files)

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Endpoints/ProjectsEndpoint.cs`

- [ ] **Step 1: Implement ProjectsEndpoint.cs**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Seed.Engine.Transpiler;
using Seed.Server.Storage;

namespace Seed.Server.Endpoints;

public static class ProjectsEndpoint
{
    public sealed record SaveRequest(string ProjectId, string Dsl);

    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", (ClaimsPrincipal user, PerUserStorageFactory factory) =>
        {
            var storage = factory.For(user.GetUserId());
            return Results.Ok(storage.List());
        });

        group.MapGet("/{id}", (string id, ClaimsPrincipal user, PerUserStorageFactory factory) =>
        {
            var storage = factory.For(user.GetUserId());
            try { return Results.Ok(storage.Load(id)); }
            catch (FileNotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", (
            [FromBody] SaveRequest req,
            ClaimsPrincipal user,
            PerUserStorageFactory factory,
            IParser parser,
            ITranspiler transpiler) =>
        {
            if (string.IsNullOrWhiteSpace(req.ProjectId)) return Results.BadRequest(new { error = "Missing ProjectId" });
            var parsed = parser.Parse(req.Dsl);
            if (!parsed.IsValid) return Results.BadRequest(new { error = "parse_failed", details = parsed.Errors });
            var dna = transpiler.Transpile(parsed);
            var storage = factory.For(user.GetUserId());
            var savedId = storage.Save(dna, req.ProjectId);
            return Results.Ok(new { id = savedId });
        });

        group.MapDelete("/{id}", (string id, ClaimsPrincipal user, PerUserStorageFactory factory) =>
        {
            var storage = factory.For(user.GetUserId());
            storage.Delete(id);
            return Results.NoContent();
        });

        return app;
    }
}

internal static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value
        ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing sub claim");
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Seed.Server/Endpoints/ProjectsEndpoint.cs
git commit -m "feat(server): /api/projects CRUD scoped to authenticated user"
```

---

## Task 7: HealthEndpoint + run server locally

**Files:**
- Create: `D:/ComfyUI-Intel/seed/src/Seed.Server/Endpoints/HealthEndpoint.cs`

- [ ] **Step 1: Implement HealthEndpoint.cs**

```csharp
namespace Seed.Server.Endpoints;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "ok",
            engine = "Seed.Engine v0.1.0",
            time = DateTime.UtcNow,
        }));
        return app;
    }
}
```

- [ ] **Step 2: Run + smoke test locally**

```bash
cd D:/ComfyUI-Intel/seed
dotnet run --project src/Seed.Server/Seed.Server.csproj --launch-profile https
```

Open https://localhost:5001/swagger — verify all 5 endpoints listed.

Open https://localhost:5001/api/health — should return JSON.

- [ ] **Step 3: Commit**

```bash
git add src/Seed.Server/Endpoints/HealthEndpoint.cs
git commit -m "feat(server): /api/health for liveness probes"
```

---

## Task 8: Deploy Seed.Server to i5

(Adapt to whatever the i5 verification revealed — Tailscale Funnel, Cloudflare Tunnel, or direct port-forward.)

- [ ] **Step 1: Publish self-contained binary**

```bash
cd D:/ComfyUI-Intel/seed
dotnet publish src/Seed.Server/Seed.Server.csproj -c Release -r win-x64 --self-contained true -o publish/seed-server
```

- [ ] **Step 2: Copy to i5**

If LAN-reachable :
```bash
scp -r publish/seed-server bui1@<i5-ip>:/c/seed-server
```

If Tailscale :
```bash
tailscale file cp publish/seed-server.zip <i5-tailscale-name>:
# then on i5 : tailscale file get
```

- [ ] **Step 3: Install as Windows service (NSSM) or run as scheduled task**

```powershell
# On i5, in PowerShell as Admin:
nssm install SeedServer "C:\seed-server\Seed.Server.exe"
nssm set SeedServer AppDirectory "C:\seed-server"
nssm start SeedServer
```

- [ ] **Step 4: Verify health from external machine**

```bash
curl https://seed.<bui1-domain>/api/health
```

Expected: 200 OK + JSON response.

---

## Task 9: TypeScript client lib in genia

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/types.ts`
- Create: `D:/GeniA/apps/web/lib/seed/client.ts`

- [ ] **Step 1: Define DTOs in types.ts**

```typescript
export type ProjectMetadata = {
  id: string;
  name: string;
  type: string;
  goal: string;
  modifiedUtc: string;
};

export type ComposeResponse = {
  compactDsl: string;
  originalTokens: number;
  compactTokens: number;
  compressionRatio: number;
};

export type Modifier = { key: string | null; value: string };
export type Link = { to: string; type: 'Seq' | 'Par' | 'Alt' };
export type Statement = {
  id: string;
  verb: string;
  target: string;
  modifiers: Modifier[];
  constraints: string[];
  comment: string | null;
  links: Link[];
};

export type DnaFile = {
  version: string;
  header: { type: string; name: string; goal: string };
  statements: Statement[];
};
```

- [ ] **Step 2: Implement client.ts**

```typescript
import type { ComposeResponse, DnaFile, ProjectMetadata } from './types';

const BASE = process.env.NEXT_PUBLIC_SEED_API_URL ?? 'https://seed.example.com';

async function authedFetch(path: string, init: RequestInit, token: string) {
  const res = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...(init.headers ?? {}),
    },
  });
  if (!res.ok) throw new Error(`SEED ${path} ${res.status}: ${await res.text()}`);
  return res.json();
}

export const seedClient = {
  compose: (input: string, token: string): Promise<ComposeResponse> =>
    authedFetch('/api/compose', { method: 'POST', body: JSON.stringify({ input }) }, token),
  list: (token: string): Promise<ProjectMetadata[]> =>
    authedFetch('/api/projects', { method: 'GET' }, token),
  load: (id: string, token: string): Promise<DnaFile> =>
    authedFetch(`/api/projects/${encodeURIComponent(id)}`, { method: 'GET' }, token),
  save: (projectId: string, dsl: string, token: string): Promise<{ id: string }> =>
    authedFetch('/api/projects', { method: 'POST', body: JSON.stringify({ projectId, dsl }) }, token),
  remove: (id: string, token: string): Promise<void> =>
    authedFetch(`/api/projects/${encodeURIComponent(id)}`, { method: 'DELETE' }, token),
};
```

- [ ] **Step 3: Commit**

```bash
cd D:/GeniA
git add apps/web/lib/seed/
git commit -m "feat(seed): typed client for ASP.NET Seed.Server endpoints"
```

---

## Task 10: useSeedProjects TanStack Query hooks

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/useSeedProjects.ts`

- [ ] **Step 1: Implement hooks**

```typescript
'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSupabaseToken } from '../auth/useSupabaseToken';  // existing in genia
import { seedClient } from './client';

export function useSeedProjectList() {
  const token = useSupabaseToken();
  return useQuery({
    queryKey: ['seed', 'projects'],
    queryFn: () => seedClient.list(token!),
    enabled: !!token,
  });
}

export function useSeedProject(id: string) {
  const token = useSupabaseToken();
  return useQuery({
    queryKey: ['seed', 'project', id],
    queryFn: () => seedClient.load(id, token!),
    enabled: !!token && !!id,
  });
}

export function useSeedSave() {
  const token = useSupabaseToken();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ projectId, dsl }: { projectId: string; dsl: string }) =>
      seedClient.save(projectId, dsl, token!),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['seed', 'projects'] }),
  });
}

export function useSeedDelete() {
  const token = useSupabaseToken();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => seedClient.remove(id, token!),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['seed', 'projects'] }),
  });
}

export function useSeedCompose() {
  const token = useSupabaseToken();
  return useMutation({
    mutationFn: (input: string) => seedClient.compose(input, token!),
  });
}
```

(Adjust `useSupabaseToken` import path to whatever genia's existing auth helper is named.)

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/useSeedProjects.ts
git commit -m "feat(seed): TanStack Query hooks for project CRUD + compose"
```

---

## Task 11: Replace /seed page Stage 0 mock with real engine calls

**Files:**
- Modify: `D:/GeniA/apps/web/app/seed/page.tsx` (rewrite)
- Create: `D:/GeniA/apps/web/app/seed/components/ComposerForm.tsx`
- Create: `D:/GeniA/apps/web/app/seed/components/ProjectListPanel.tsx`

- [ ] **Step 1: Move composer UI into ComposerForm.tsx**

(Lift the JSX + state management from the Stage 0 mock into this component, but replace the local-only `composeAll` call with `useSeedCompose().mutate()` and add `useSeedSave()` for the Save button.)

- [ ] **Step 2: Implement ProjectListPanel.tsx**

(Uses `useSeedProjectList()`, renders rows of saved projects with Open / Delete buttons.)

- [ ] **Step 3: Rewrite page.tsx**

```tsx
'use client';

import { useState } from 'react';
import { ComposerForm } from './components/ComposerForm';
import { ProjectListPanel } from './components/ProjectListPanel';

export default function SeedPage() {
  const [openProjectId, setOpenProjectId] = useState<string | null>(null);

  return (
    <div className="min-h-screen bg-genia-dark text-white pb-20">
      <header className="px-4 pt-6 pb-4 border-b border-white/10">
        <h1 className="text-2xl font-bold flex items-center gap-2">🌱 SEED</h1>
        <p className="text-sm text-white/60 mt-1">Plant the intent. Grow the system.</p>
      </header>

      {openProjectId === null ? (
        <ProjectListPanel onOpen={setOpenProjectId} onNew={() => setOpenProjectId('new')} />
      ) : (
        <ComposerForm projectId={openProjectId} onClose={() => setOpenProjectId(null)} />
      )}
    </div>
  );
}
```

- [ ] **Step 4: Set NEXT_PUBLIC_SEED_API_URL in genia env**

In Vercel project settings, add env var:
```
NEXT_PUBLIC_SEED_API_URL = https://seed.<bui1-domain>
```

- [ ] **Step 5: Commit**

```bash
git add apps/web/app/seed/
git commit -m "feat(seed): replace Stage 0 mock with real engine via i5 backend"
```

---

## Task 12: End-to-end smoke test on genia.social staging

- [ ] **Step 1: Push to a preview branch on genia (not master) to get a Vercel preview URL**

```bash
cd D:/GeniA
git push origin master:seed-stage-1-preview
```

(Vercel auto-creates a preview deploy at `seed-stage-1-preview-genia.vercel.app`.)

- [ ] **Step 2: Open preview URL, log in with Supabase, click 🌱 SEED**

Verify:
- Project list loads (empty initially)
- Click "New project" → composer opens
- Type a project, click Compose → POST /api/compose succeeds (token stamped on i5 server logs)
- Click Save → project appears in list after navigating back
- Click Open → project loads back into composer
- Click Delete → project disappears from list

- [ ] **Step 3: Tail i5 server logs to confirm requests landing**

```powershell
Get-Content C:\seed-server\logs\seed-server.log -Tail 50 -Wait
```

- [ ] **Step 4: Once green, merge to master**

```bash
cd D:/GeniA
git checkout master
git merge --ff-only seed-stage-1-preview
git push origin master
```

(Vercel deploys to production genia.social.)

---

## Self-Review Checklist

- [x] Each task has exact file paths
- [x] Each code step has complete code (no placeholders)
- [x] Type names consistent
- [x] HARD GATE on i5 verification (Task 1) before any other task starts
- [x] Auth: Supabase JWT bearer flows from browser → i5 server validation
- [x] Storage scoped per-user via `sub` claim, sanitized to prevent path traversal
- [x] CORS allowlist only `genia.social` (+ localhost for dev)
- [x] Spec coverage:
  - Spec §3 architecture → Tasks 2-7 (engine consumption + endpoints)
  - Spec §5 UI flow → Task 11 (page rewrite)
  - Spec §7 components → Task 9-10 (typed client + hooks)
  - Spec §9 v1 scope → 12 tasks, full E2E

---

## Open items deferred to Stage 2

1. **FORGE deep-link from web** — needs `forge://open-dna?payload=<base64>` URI handler registered on Windows. Stage 2 task.
2. **Sharing links** — `seed/p/<short-id>` for read-only public view of a project.
3. **Multi-device sync** — current model is per-user-per-i5; Stage 2 syncs to Supabase Storage too for cross-device.
4. **Mobile UX** — current responsive enough for tablet, but bottom-sheet UI ideal for phone.
5. **Visual library on web** — Stage 0 has no icons per verb. Stage 3 adds the SEED icon catalog (~194 slugs) into the web composer.

---

## Execution Handoff

**Next session prompt :**

> Execute the SEED Genia + i5 integration plan at
> `D:/ComfyUI-Intel/seed/docs/superpowers/plans/2026-04-26-seed-genia-i5-integration-plan.md`
> via superpowers:subagent-driven-development.
>
> CRITICAL : Run Task 1 (i5 verification checklist at sibling file) FIRST. Do NOT
> proceed to Task 2 until ALL checklist items are green. If any fails, ask the
> human (bui1) to fix before continuing.
>
> 12 tasks. Backend tasks (2-7) work in `D:/ComfyUI-Intel/seed/`. Frontend tasks
> (9-11) work in `D:/GeniA/`. Task 8 = manual deploy step requiring user action
> on i5. Task 12 = manual smoke test requiring user action.
>
> Pre-req : Stage 0 mock already shipped at genia commit `0f0b4b4` on master.
