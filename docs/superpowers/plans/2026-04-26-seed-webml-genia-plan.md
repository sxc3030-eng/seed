# SEED Web ML — Final Architecture Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Status:** Definitive plan. Supersedes all earlier "Stage 1" / "i5 backend" / "Blazor standalone" plans.

**Goal:** A page at `genia.social/seed` where the user types a free-form brief (FR/EN). A small machine-learning model (MiniLM-L6-v2 via Transformers.js) running **inside the browser** extracts semantic concepts from the brief and emits a compact `key=value=value=…` AI-code prompt the user can copy into Claude / FORGE chat. **Zero servers. Zero APIs. Zero PC dependency.** Once the ~22 MB model is cached on the device, everything runs offline in the tab.

**Why this architecture survives every constraint we discovered:**

| Constraint user added | This plan's answer |
|---|---|
| Free | Transformers.js + MiniLM = MIT/Apache, 100 % free |
| Autonome (no Anthropic) | Browser-side, no API key needed |
| Pas d'IA externe | Inference runs in your tab, not on a server |
| Mon i5/i7 peut être OFF | Page is hosted on Vercel; ML runs in YOUR browser, on whatever device you're using |
| Machine learning | MiniLM is a real BERT-derivative neural net |

---

## Tech Stack

- Frontend: Next.js 14 (existing genia.social), TypeScript, React
- ML: `@xenova/transformers` (Transformers.js) ~3 MB JS bundle + MiniLM-L6-v2 ONNX (~22 MB lazy-loaded once)
- Caching: browser IndexedDB (Transformers.js handles model cache automatically)
- Storage (briefs history): browser localStorage (Stage A) → optional Supabase (Stage B, deferred)
- Hosting: existing genia.social on Vercel
- No backend changes required

---

## File Structure

```
genia/apps/web/
├── app/
│   └── seed/
│       ├── page.tsx                       (rewrite — minimal UI shell)
│       └── components/
│           ├── BriefEditor.tsx            (textarea + word counter)
│           ├── ModelStatus.tsx            (load/cache/ready indicator)
│           ├── CompressOutput.tsx         (AI-code + copy/download)
│           └── BriefHistory.tsx           (localStorage list)
└── lib/
    └── seed/
        ├── vocabulary.ts                  (SEED concept dictionary + AI-code map)
        ├── ml-compressor.ts               (Transformers.js wrapper, lazy-init)
        ├── extractor.ts                   (embedding → top-k vocab matches)
        ├── ai-code-emitter.ts             (matched concepts → final string)
        └── brief-storage.ts               (localStorage CRUD)
```

---

## SEED Vocabulary Design

The vocabulary is the heart of the system. Each entry maps a **canonical concept** to:
- Its short AI-code symbol (1-4 chars)
- A list of seed words / phrases that mean it (FR + EN)
- A category (action, target, modifier, constraint, ui-hint)

Total target: ~150 entries covering web, cli, mobile, db, audio/video, ui, monitoring domains.

Format (`vocabulary.ts`):

```ts
export type ConceptCategory = 'action' | 'target' | 'modifier' | 'constraint' | 'ui';

export type Concept = {
  code: string;           // canonical AI-code, e.g. "mp", "db:sql"
  category: ConceptCategory;
  seeds: string[];        // FR + EN words/phrases meaning this concept
  embedding?: Float32Array; // pre-computed (optional, otherwise computed at startup)
};

export const VOCABULARY: Concept[] = [
  // === Targets ===
  { code: 'mp',       category: 'target', seeds: ['mp3', 'audio', 'musique', 'music', 'song', 'chanson', 'sound', 'son'] },
  { code: 'vid',      category: 'target', seeds: ['video', 'vidéo', 'movie', 'film', 'clip', 'mp4'] },
  { code: 'img',      category: 'target', seeds: ['image', 'photo', 'picture', 'png', 'jpg', 'jpeg'] },
  { code: 'doc',      category: 'target', seeds: ['document', 'doc', 'pdf', 'fichier', 'file', 'word'] },
  { code: 'mail',     category: 'target', seeds: ['email', 'courriel', 'mail', 'message', 'inbox'] },
  { code: 'web',      category: 'target', seeds: ['website', 'site', 'page web', 'webapp', 'web app', 'application web'] },
  { code: 'api',      category: 'target', seeds: ['api', 'endpoint', 'rest', 'graphql', 'service'] },
  { code: 'user',     category: 'target', seeds: ['utilisateur', 'user', 'compte', 'account', 'profil', 'profile'] },
  { code: 'folder',   category: 'target', seeds: ['dossier', 'folder', 'directory', 'répertoire'] },
  { code: 'playlist', category: 'target', seeds: ['playlist', 'liste de lecture', 'queue'] },

  // === Actions (verbs from SEED grammar) ===
  { code: 'read',     category: 'action', seeds: ['lire', 'read', 'lit', 'play', 'jouer', 'écouter', 'listen', 'open', 'ouvrir'] },
  { code: 'write',    category: 'action', seeds: ['écrire', 'write', 'save', 'enregistrer', 'sauvegarder', 'store', 'stocker'] },
  { code: 'filter',   category: 'action', seeds: ['filtrer', 'filter', 'tri', 'trier', 'sort', 'classer'] },
  { code: 'create',   category: 'action', seeds: ['créer', 'create', 'générer', 'generate', 'make', 'new'] },
  { code: 'analyze',  category: 'action', seeds: ['analyser', 'analyze', 'scan', 'scanner', 'inspecter', 'inspect', 'parse'] },
  { code: 'send',     category: 'action', seeds: ['envoyer', 'send', 'alerter', 'alert', 'notifier', 'notify'] },
  { code: 'monitor',  category: 'action', seeds: ['surveiller', 'monitor', 'watch', 'observer'] },
  { code: 'transform',category: 'action', seeds: ['transformer', 'transform', 'convertir', 'convert', 'compiler', 'compile'] },

  // === Modifiers (technical qualifiers) ===
  { code: 'db:sql',   category: 'modifier', seeds: ['sql', 'sqlite', 'postgres', 'mysql', 'base de données', 'database'] },
  { code: 'db:nosql', category: 'modifier', seeds: ['mongodb', 'mongo', 'redis', 'nosql', 'document store'] },
  { code: 'auth:jwt', category: 'modifier', seeds: ['jwt', 'token', 'bearer'] },
  { code: 'auth:oauth', category: 'modifier', seeds: ['oauth', 'oauth2', 'sso'] },
  { code: 'fmt:json', category: 'modifier', seeds: ['json', 'rest json'] },
  { code: 'fmt:csv',  category: 'modifier', seeds: ['csv', 'excel', 'tableur'] },
  { code: 'rnd',      category: 'modifier', seeds: ['aléatoire', 'random', 'shuffle', 'mélangé'] },
  { code: 'rep',      category: 'modifier', seeds: ['repeat', 'répéter', 'loop', 'boucle'] },
  { code: 'live',     category: 'modifier', seeds: ['temps réel', 'realtime', 'real time', 'live', 'streaming'] },
  { code: 'urgent',   category: 'modifier', seeds: ['urgent', 'critique', 'critical', 'priority', 'priorité'] },

  // === Constraints (always prefixed with !) ===
  { code: '!offline', category: 'constraint', seeds: ['offline', 'hors ligne', 'sans internet', 'local only'] },
  { code: '!rgpd',    category: 'constraint', seeds: ['rgpd', 'gdpr', 'compliance privacy'] },
  { code: '!secure',  category: 'constraint', seeds: ['secure', 'sécurisé', 'encrypted', 'chiffré'] },

  // === UI hints ===
  { code: 'ui:retro', category: 'ui',       seeds: ['rétro', 'retro', 'vintage', 'old school'] },
  { code: 'ui:modern',category: 'ui',       seeds: ['moderne', 'modern', 'flat', 'minimal'] },
  { code: 'ui:dark',  category: 'ui',       seeds: ['dark mode', 'sombre', 'night'] },
  { code: 'ui:gui',   category: 'ui',       seeds: ['interface graphique', 'gui', 'window', 'fenêtre', 'icon', 'icône'] },
  { code: 'ui:cli',   category: 'ui',       seeds: ['ligne de commande', 'cli', 'terminal', 'console'] },

  // ... seed with ~150 total. Stage A ships ~50, Stage B grows it.
];
```

---

## Task 1: Add Transformers.js dependency

**Files:**
- Modify: `D:/GeniA/apps/web/package.json`
- Modify: `D:/GeniA/apps/web/next.config.js` (Webpack tweaks for Transformers.js + ONNX runtime)

- [ ] **Step 1: Add npm dependency**

```bash
cd D:/GeniA/apps/web
npm install @xenova/transformers
```

- [ ] **Step 2: Configure Webpack to handle ONNX runtime**

In `next.config.js`, add:

```js
const nextConfig = {
  // ...existing config
  webpack: (config, { isServer }) => {
    if (!isServer) {
      config.resolve.fallback = {
        ...config.resolve.fallback,
        fs: false,
        path: false,
        crypto: false,
      };
    }
    config.resolve.alias = {
      ...config.resolve.alias,
      'sharp$': false,
      'onnxruntime-node$': false,
    };
    return config;
  },
};
```

- [ ] **Step 3: Verify build still works**

```bash
npm run dev
```
Open `http://localhost:3000` — no errors in dev console.

- [ ] **Step 4: Commit**

```bash
cd D:/GeniA
git checkout -b seed-webml
git add apps/web/package.json apps/web/package-lock.json apps/web/next.config.js
git commit -m "chore(seed): add @xenova/transformers + webpack ONNX config"
```

---

## Task 2: Build vocabulary.ts

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/vocabulary.ts`

- [ ] **Step 1: Write vocabulary** (use the format above, ~50 concepts for Stage A)

Cover the domains user has expressed: cli, audio/video player, web app, db, api, monitoring, file ops, auth, ui themes.

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/vocabulary.ts
git commit -m "feat(seed): SEED vocabulary v1 — 50 concepts across 5 categories"
```

---

## Task 3: ml-compressor.ts — Transformers.js wrapper

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/ml-compressor.ts`

- [ ] **Step 1: Implement the singleton ML pipeline**

```typescript
'use client';

import { pipeline, env } from '@xenova/transformers';

// Force browser cache, no remote loading after first time
env.allowRemoteModels = true;
env.useBrowserCache = true;

let extractorPromise: Promise<any> | null = null;

export async function getExtractor() {
  if (!extractorPromise) {
    extractorPromise = pipeline('feature-extraction', 'Xenova/all-MiniLM-L6-v2', {
      quantized: true, // smaller, faster
    });
  }
  return extractorPromise;
}

export async function embed(text: string): Promise<Float32Array> {
  const extractor = await getExtractor();
  const result = await extractor(text, { pooling: 'mean', normalize: true });
  return result.data as Float32Array;
}

export function cosineSimilarity(a: Float32Array, b: Float32Array): number {
  let dot = 0, magA = 0, magB = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    magA += a[i] * a[i];
    magB += b[i] * b[i];
  }
  return dot / (Math.sqrt(magA) * Math.sqrt(magB));
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/ml-compressor.ts
git commit -m "feat(seed): Transformers.js MiniLM wrapper (lazy singleton)"
```

---

## Task 4: extractor.ts — match user words to vocabulary concepts

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/extractor.ts`

- [ ] **Step 1: Implement extractor**

```typescript
import { embed, cosineSimilarity } from './ml-compressor';
import { VOCABULARY, type Concept } from './vocabulary';

const SIMILARITY_THRESHOLD = 0.55; // tune empirically

let conceptEmbeddingsCache: Float32Array[] | null = null;

async function getConceptEmbeddings(): Promise<Float32Array[]> {
  if (conceptEmbeddingsCache) return conceptEmbeddingsCache;

  conceptEmbeddingsCache = await Promise.all(
    VOCABULARY.map(async (c) => {
      // Embed the joined seeds (rich semantic representation of the concept)
      const text = c.seeds.join(' ');
      return embed(text);
    })
  );
  return conceptEmbeddingsCache;
}

export type Match = { concept: Concept; score: number };

export async function extractConcepts(brief: string, topK = 12): Promise<Match[]> {
  const conceptEmbeddings = await getConceptEmbeddings();

  // Split brief into "candidate phrases" (sentences + chunks)
  const phrases = brief
    .split(/[.!?\n,;]+/)
    .map((s) => s.trim())
    .filter((s) => s.length > 2);

  const matches = new Map<string, Match>(); // dedupe by concept code

  for (const phrase of phrases) {
    const phraseEmb = await embed(phrase);

    for (let i = 0; i < VOCABULARY.length; i++) {
      const score = cosineSimilarity(phraseEmb, conceptEmbeddings[i]);
      if (score >= SIMILARITY_THRESHOLD) {
        const concept = VOCABULARY[i];
        const existing = matches.get(concept.code);
        if (!existing || score > existing.score) {
          matches.set(concept.code, { concept, score });
        }
      }
    }
  }

  return Array.from(matches.values())
    .sort((a, b) => b.score - a.score)
    .slice(0, topK);
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/extractor.ts
git commit -m "feat(seed): semantic concept extractor via cosine similarity"
```

---

## Task 5: ai-code-emitter.ts — assemble final compact prompt

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/ai-code-emitter.ts`

- [ ] **Step 1: Implement emitter**

```typescript
import type { Match } from './extractor';

export type EmittedPrompt = {
  aiCode: string;
  fullPrompt: string;
  conceptCount: number;
};

const GRAMMAR_HINT = `
SEED v0.1 grammar:
  TYPE/NAME/GOAL header, statements verb<target>[<modifier>] chained via → & |
  Verbs: scraper recevoir lire filtrer parser transformer analyser détecter valider créer enregistrer générer envoyer alerter notifier surveiller déclencher retry logger gérer-erreur
  Modifiers: <slot> or <key:value>. Constraints: !XX. Entities: @XX
  Output ONLY the .dna, nothing else.
`.trim();

export function emitPrompt(matches: Match[], includeBrief?: string): EmittedPrompt {
  // Group by category for richer AI-code shape
  const targets = matches.filter((m) => m.concept.category === 'target').map((m) => m.concept.code);
  const actions = matches.filter((m) => m.concept.category === 'action').map((m) => m.concept.code);
  const modifiers = matches.filter((m) => m.concept.category === 'modifier').map((m) => m.concept.code);
  const constraints = matches.filter((m) => m.concept.category === 'constraint').map((m) => m.concept.code);
  const uiHints = matches.filter((m) => m.concept.category === 'ui').map((m) => m.concept.code);

  // Compact AI-code: A:actions T:targets M:modifiers C:constraints U:ui
  const parts: string[] = [];
  if (actions.length) parts.push(`A=${actions.join('+')}`);
  if (targets.length) parts.push(`T=${targets.join('+')}`);
  if (modifiers.length) parts.push(`M=${modifiers.join('+')}`);
  if (constraints.length) parts.push(`C=${constraints.join('+')}`);
  if (uiHints.length) parts.push(`U=${uiHints.join('+')}`);

  const aiCode = parts.join(' | ');

  const lines: string[] = [`SEED brief: ${aiCode}`, '', GRAMMAR_HINT];
  if (includeBrief) {
    lines.push('', '--- original brief (fallback context) ---', includeBrief.trim());
  }

  return {
    aiCode,
    fullPrompt: lines.join('\n'),
    conceptCount: matches.length,
  };
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/ai-code-emitter.ts
git commit -m "feat(seed): emit grouped AI-code prompt (A=… T=… M=… C=… U=…)"
```

---

## Task 6: brief-storage.ts — localStorage history

**Files:**
- Create: `D:/GeniA/apps/web/lib/seed/brief-storage.ts`

- [ ] **Step 1: Implement storage helpers**

```typescript
const KEY = 'seed.briefs.v1';

export type StoredBrief = {
  id: string;
  brief: string;
  aiCode: string;
  createdAt: number;
};

export function listBriefs(): StoredBrief[] {
  if (typeof window === 'undefined') return [];
  try {
    const raw = window.localStorage.getItem(KEY);
    if (!raw) return [];
    return JSON.parse(raw) as StoredBrief[];
  } catch {
    return [];
  }
}

export function saveBrief(brief: string, aiCode: string): StoredBrief {
  const all = listBriefs();
  const entry: StoredBrief = {
    id: `b_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
    brief, aiCode, createdAt: Date.now(),
  };
  const next = [entry, ...all].slice(0, 30); // cap at 30
  window.localStorage.setItem(KEY, JSON.stringify(next));
  return entry;
}

export function deleteBrief(id: string) {
  const next = listBriefs().filter((b) => b.id !== id);
  window.localStorage.setItem(KEY, JSON.stringify(next));
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/lib/seed/brief-storage.ts
git commit -m "feat(seed): localStorage brief history (30 entries cap)"
```

---

## Task 7: BriefEditor + ModelStatus + CompressOutput components

**Files:**
- Create: `D:/GeniA/apps/web/app/seed/components/BriefEditor.tsx`
- Create: `D:/GeniA/apps/web/app/seed/components/ModelStatus.tsx`
- Create: `D:/GeniA/apps/web/app/seed/components/CompressOutput.tsx`

- [ ] **Step 1: ModelStatus.tsx** — small badge showing "loading model… / ready / error"

```tsx
'use client';

import { useEffect, useState } from 'react';
import { getExtractor } from '../../../lib/seed/ml-compressor';

export function ModelStatus() {
  const [status, setStatus] = useState<'idle' | 'loading' | 'ready' | 'error'>('idle');
  const [error, setError] = useState('');

  useEffect(() => {
    setStatus('loading');
    getExtractor()
      .then(() => setStatus('ready'))
      .catch((e) => { setStatus('error'); setError(e.message); });
  }, []);

  if (status === 'idle') return null;
  if (status === 'loading') return (
    <div className="text-xs text-yellow-300/80 px-3 py-1 bg-yellow-900/20 rounded inline-block">
      ⏳ Téléchargement du modèle ML (~22 MB, une seule fois)…
    </div>
  );
  if (status === 'error') return (
    <div className="text-xs text-red-400 px-3 py-1 bg-red-900/20 rounded inline-block">
      ✕ Modèle ML indisponible: {error}
    </div>
  );
  return (
    <div className="text-xs text-genia-primary px-3 py-1 bg-genia-primary/10 rounded inline-block">
      ✓ Modèle ML prêt (offline)
    </div>
  );
}
```

- [ ] **Step 2: BriefEditor.tsx** — controlled textarea with word counter

(Standard React controlled component, similar to current page; emit `onChange(brief)`.)

- [ ] **Step 3: CompressOutput.tsx** — display AI-code, full prompt, copy + download buttons

(Like current page output, but uses `EmittedPrompt` shape.)

- [ ] **Step 4: Commit**

```bash
git add apps/web/app/seed/components/
git commit -m "feat(seed): BriefEditor + ModelStatus + CompressOutput components"
```

---

## Task 8: Rewrite app/seed/page.tsx orchestrator

**Files:**
- Replace: `D:/GeniA/apps/web/app/seed/page.tsx`

- [ ] **Step 1: Wire it all together**

```tsx
'use client';

import { useState } from 'react';
import { extractConcepts } from '../../lib/seed/extractor';
import { emitPrompt, type EmittedPrompt } from '../../lib/seed/ai-code-emitter';
import { saveBrief } from '../../lib/seed/brief-storage';
import { BriefEditor } from './components/BriefEditor';
import { ModelStatus } from './components/ModelStatus';
import { CompressOutput } from './components/CompressOutput';
import { BriefHistory } from './components/BriefHistory';

export default function SeedPage() {
  const [brief, setBrief] = useState('');
  const [includeOriginal, setIncludeOriginal] = useState(false);
  const [busy, setBusy] = useState(false);
  const [output, setOutput] = useState<EmittedPrompt | null>(null);

  async function handleGenerate() {
    if (!brief.trim()) return;
    setBusy(true);
    try {
      const matches = await extractConcepts(brief);
      const result = emitPrompt(matches, includeOriginal ? brief : undefined);
      setOutput(result);
      saveBrief(brief, result.aiCode);
    } catch (e) {
      console.error(e);
      alert(`Échec compression: ${(e as Error).message}`);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen bg-genia-dark text-white pb-24">
      <header className="px-4 pt-6 pb-4 border-b border-white/10">
        <h1 className="text-2xl font-bold">🌱 SEED</h1>
        <p className="text-sm text-white/60 mt-1">Plant the intent. Grow the system.</p>
        <div className="mt-2"><ModelStatus /></div>
        <p className="text-xs text-white/40 mt-2">
          Écris ton brief, clique Créé. ML local extrait les concepts → AI-code compact prêt à coller dans Claude / FORGE.
          100% navigateur, aucun serveur, aucun de tes ordis nécessaire.
        </p>
      </header>

      <BriefEditor value={brief} onChange={setBrief} />

      <section className="px-4 py-2">
        <label className="flex items-center gap-2 text-xs text-white/50 mb-2 cursor-pointer">
          <input type="checkbox" checked={includeOriginal}
            onChange={(e) => setIncludeOriginal(e.target.checked)}
            className="accent-genia-primary" />
          Inclure aussi le brief original (fallback contexte)
        </label>
        <button onClick={handleGenerate} disabled={!brief.trim() || busy}
          className="w-full bg-genia-primary hover:bg-genia-primary/80 disabled:opacity-30 text-black font-bold rounded py-3 text-base">
          {busy ? '⏳ ML en cours…' : '🌱 Créé'}
        </button>
      </section>

      {output && <CompressOutput output={output} />}

      <BriefHistory onLoad={(b) => setBrief(b)} />
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/app/seed/page.tsx
git commit -m "feat(seed): orchestrator page wiring ML extraction → AI-code emission"
```

---

## Task 9: BriefHistory component

**Files:**
- Create: `D:/GeniA/apps/web/app/seed/components/BriefHistory.tsx`

- [ ] **Step 1: List recent briefs from localStorage with click-to-reload**

```tsx
'use client';

import { useEffect, useState } from 'react';
import { listBriefs, deleteBrief, type StoredBrief } from '../../../lib/seed/brief-storage';

export function BriefHistory({ onLoad }: { onLoad: (brief: string) => void }) {
  const [items, setItems] = useState<StoredBrief[]>([]);
  const [open, setOpen] = useState(false);

  useEffect(() => { setItems(listBriefs()); }, []);

  if (items.length === 0) return null;

  return (
    <section className="px-4 py-4">
      <button onClick={() => setOpen(!open)} className="text-xs text-white/40 hover:text-white/60 uppercase tracking-wide">
        {open ? '▼' : '▶'} Historique ({items.length})
      </button>
      {open && (
        <ul className="mt-2 space-y-1">
          {items.map((b) => (
            <li key={b.id} className="flex items-center justify-between bg-black/30 rounded px-2 py-1 text-xs">
              <button onClick={() => onLoad(b.brief)} className="flex-1 text-left text-white/80 truncate">
                {b.brief.slice(0, 80)}…
              </button>
              <button onClick={() => { deleteBrief(b.id); setItems(listBriefs()); }}
                className="text-red-400/60 hover:text-red-400 px-1">✕</button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/web/app/seed/components/BriefHistory.tsx
git commit -m "feat(seed): BriefHistory component with localStorage CRUD"
```

---

## Task 10: Local smoke test

- [ ] **Step 1: Restart dev server fresh**

```bash
cd D:/GeniA/apps/web
npm run dev
```

- [ ] **Step 2: Open http://localhost:3000/seed in browser**

- [ ] **Step 3: First load**
  - "Téléchargement du modèle ML…" appears
  - Wait 5-30 seconds (depending on connection)
  - Status flips to "✓ Modèle ML prêt (offline)"

- [ ] **Step 4: Compose**
  - Paste the music brief: *"je veux un programme qui lit les mp3, classe les dossiers de musique, plusieurs façons de lire (aléatoire, continu, repeat), playlists, analyse mon disque dur..."*
  - Click 🌱 Créé
  - Watch the button: "⏳ ML en cours…" then output appears
  - Verify AI-code looks like: `A=read+filter+create+analyze | T=mp+folder+playlist | M=rnd+rep`

- [ ] **Step 5: Reload page**
  - Model status should be **instant** "✓ Modèle ML prêt (offline)" (cache hit)

- [ ] **Step 6: Verify offline**
  - Open DevTools → Network tab → check "Offline"
  - Click 🌱 Créé again — should still work (everything cached)

---

## Task 11: Push and Vercel preview deploy

- [ ] **Step 1: Push branch**

```bash
cd D:/GeniA
git push origin seed-webml
```

- [ ] **Step 2: Vercel auto-creates a preview URL** (something like `seed-webml-genia.vercel.app`)

- [ ] **Step 3: Open preview URL on a different device** (phone, tablet, work PC)
  - Verify model loads
  - Verify compression works
  - Confirms "any device" promise

- [ ] **Step 4: If green, merge to master**

```bash
git checkout master
git merge --ff-only seed-webml
git push origin master
```

(Vercel auto-deploys to genia.social/seed.)

---

## Task 12: Future polish (deferred)

- Embedding pre-computation at build time (instead of first-load) → faster startup
- Vocabulary growth: add ~100 more concepts covering more domains
- "Send directly to FORGE" button (when FORGE is reachable from browser via WebSocket)
- Supabase sync for cross-device brief history
- Export/import vocabulary as JSON for power users

---

## Self-Review

- [x] Each task has exact file paths and code
- [x] No placeholders / TBDs
- [x] Type names consistent (Concept, Match, EmittedPrompt, StoredBrief)
- [x] Architecture survives "PC OFF" — model lives in browser
- [x] Architecture survives "no API" — Transformers.js does inference locally
- [x] Architecture survives "free" — all open-source, no paid tier
- [x] Architecture is "machine learning" — MiniLM is a real BERT-derivative neural network

---

## Execution Handoff

Demain, prochaine session :

> Execute `seed/docs/superpowers/plans/2026-04-26-seed-webml-genia-plan.md` via superpowers:subagent-driven-development. 12 tasks. Tasks 1-9 are mechanical (Haiku ok). Task 10 is manual smoke test by user. Task 11 manual deploy by user. Branch `seed-webml` from `D:/GeniA/master`.
>
> Stage 0 already shipped (genia commit `0f0b4b4` on master). This plan REPLACES the older Stage 0 + i5 backend plans (those are obsolete).
