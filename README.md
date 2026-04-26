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

🌱 **Design phase** — v1 implementation not started.

See [`docs/superpowers/specs/2026-04-25-seed-design.md`](docs/superpowers/specs/2026-04-25-seed-design.md) for the full design document.

## Architecture (planned)

- **Engine** (shared lib) : TokenDB, Composer, Parser, Transpiler, Compressor, Storage
- **Host A** (v1) : module inside FORGE — composer UI + 3D rendering of `.dna`
- **Host B** (palier 2) : standalone web SPA — copy DSL / download `.dna` / push to FORGE

## License

Private repo for now. Open-source release planned for v2 under Apache 2.0 (open-core model — engine + spec free, FORGE integration + SaaS commercial).

## Author

bui1 (Simon Cantin) — designed in collaboration with Claude (Anthropic).
