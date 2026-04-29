# SEED

> *Plant the intent. Grow the system.*

**SEED** is a DSL-based brief compactor that compresses prose project intentions into dense token chains for LLMs — and generates `.dna` files for 3D visualization in [FORGE](https://github.com/sxc3030-eng).

## Why

A typical project brief sent to an LLM is 200+ words of prose, which:

- **Wastes tokens** (cost + latency)
- **Carries ambiguity** (synonyms, fluff, filler)
- **Is hard to version** (diff a paragraph vs diff a structured chain)

SEED transforms:

> *"j'aimerais un programme qui filtre mes mails par ordre de pertinence et les enregistre, puis m'alerte sur Slack"* (~30 words)

into:

```
filtrer <mail> <pertinence> → enregistrer <db> & alerter <slack>
```

(~12 tokens — **~10× compression**)

## Status

🌱 **v0.1.0 — Engine ready** (2026-04-25)

The engine library is complete and tested. Six units (TokenDB, Composer, Parser, Transpiler, Compressor, Storage) compile a CLI-grammar project intent into:

- A compressed DSL string (sent to LLMs — measured ≥ 2× compression on real prose)
- A canonical `.dna` JSON file (consumed by FORGE for 3D viz, separate plan)

**Test coverage:** 47 tests, all green (`dotnet test`).

**Build:**
```bash
dotnet build      # builds Seed.Engine class library (.NET 8)
dotnet test       # runs full test suite
```

**Next:** FORGE integration plan (Godot panel UI + 3D renderer adapter that consumes `.dna` files).

## Documents

- Design : [`docs/superpowers/specs/2026-04-25-seed-design.md`](docs/superpowers/specs/2026-04-25-seed-design.md)
- Plan : [`docs/superpowers/plans/2026-04-25-seed-engine-plan.md`](docs/superpowers/plans/2026-04-25-seed-engine-plan.md)
- Visual library catalog : [`docs/visual-library/icon-catalog.md`](docs/visual-library/icon-catalog.md)

## Architecture

- **Engine** (`src/Seed.Engine/`) — pure C# .NET 8 class library, no UI/host deps :
  - `TokenDb` — embedded JSON registry, verb/modifier validation
  - `Composer` — UI choices → DSL string
  - `Parser` — DSL string → typed AST + errors
  - `Transpiler` — AST → canonical `.dna` JSON
  - `Compressor` — `.dna` → minimal DSL for LLM (strips comments, normalizes whitespace)
  - `Storage` — CRUD on `.dna` files (JSON file storage; SQLite alternative later if needed)
- **Host A** (palier 1) : module inside FORGE — composer UI + 3D rendering of `.dna` (separate plan)
- **Host B** (palier 2) : standalone web SPA via Blazor Wasm — same engine, browser host

## License

Private repo for now. Open-source release planned for v2 under Apache 2.0 (open-core model — engine + spec free, FORGE integration + SaaS commercial).

## Author

bui1 (Simon Cantin) — designed in collaboration with Claude (Anthropic).

---

### Method

Architecture-first, AI-paired. Designed and shipped in a **single April 2026 sprint** with **Claude (Opus 4.6)** as paired implementation and audit partner. Each commit cross-audited: code review, dependency check, test-coverage gate. v0.1.0 ships with 47 passing tests across the six engine units (TokenDB, Composer, Parser, Transpiler, Compressor, Storage).

---
