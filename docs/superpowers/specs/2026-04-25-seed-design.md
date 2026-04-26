# SEED — Design Document

**Date** : 2026-04-25
**Auteur** : bui1 (Simon Cantin) + Claude (brainstorming partner)
**Status** : Design — pending implementation plan
**Codename** : SEED — *Plant the intent. Grow the system.*

---

## 1. Vision & objectif

SEED est un **compositeur de briefs en DSL** (Domain-Specific Language) qui sert de pré-processeur entre une intention humaine floue et un LLM (ou le générateur `.dna` de FORGE).

Il transforme :

> *"j'aimerais un programme qui filtre mes mails par ordre de pertinence et les enregistre, puis m'alerte sur Slack"* (~30 mots de prose)

en :

```
filtrer <mail> <pertinence> → enregistrer <db> & alerter <slack>
```

(~12 tokens DSL — **compression ~10×**)

### Promesses mesurables

- **Économie de tokens** : ratio de compression ≥ 10× sur corpus de référence
- **Vitesse de réponse LLM** : input plus court = inférence plus rapide
- **Précision** : grammaire fermée élimine l'ambiguïté que la prose introduit
- **Bidirectionnalité** : la même chaîne DSL est lue par Claude *et* rendue en 3D dans FORGE

### Pourquoi maintenant

1. Le coût des tokens reste un facteur limitant pour les sessions longues avec Claude
2. FORGE a besoin d'un canal d'entrée structuré pour générer ses scènes 3D
3. Strategic backup : si Anthropic n'embarque pas le partenariat FORGE, SEED standalone devient un funnel pour attirer les développeurs vers l'écosystème

---

## 2. Target user (v1)

**Persona unique pour v1** : `bui1` (Simon Cantin)

- Architecte multi-IA, dev autonome, francophone
- Lance plusieurs projets en parallèle (FORGE, ISB, audits clients)
- Travaille déjà dans FORGE quotidiennement
- A besoin de cadrer rapidement de nouvelles idées sans gaspiller des sessions Claude entières en clarifications

### Vision long terme (palier 2+)

ABCD personas envisagés une fois la v1 prouvée :
- **B** : devs solo / indie hackers qui vibe-codent
- **C** : founders non-tech qui ont une idée business
- **D** : agences / consultants qui doivent extraire des specs de clients vagues

V1 ne tente pas de servir B/C/D. **YAGNI ruthlessly**.

---

## 3. Architecture d'ensemble

```
┌────────────────────────────────────────────────────────────┐
│              MOTEUR PARTAGÉ (lib indépendante)             │
│                                                            │
│  ┌──────────┐   ┌────────────┐   ┌──────────┐  ┌────────┐ │
│  │ TokenDB  │←─→│  Composer  │→  │ Parser   │→ │ AST    │ │
│  │ (vocab)  │   │ (UI→DSL)   │   │ (DSL→AST)│  │        │ │
│  └──────────┘   └────────────┘   └──────────┘  └────┬───┘ │
│                                                     │     │
│              ┌──────────────┐    ┌──────────┐      │     │
│              │ Compressor   │←───│ Transpiler│←─────┘     │
│              │ (.dna→DSL    │    │ (AST→.dna)│            │
│              │  pour LLM)   │    │           │            │
│              └──────┬───────┘    └─────┬─────┘            │
└─────────────────────┼──────────────────┼──────────────────┘
                      ↓                  ↓
            ┌──────────────────┐  ┌──────────────────┐
            │  HOST A : FORGE  │  │  HOST B : WEB    │
            │  - Panel Godot   │  │  - SPA           │
            │  - Renderer 3D   │  │  - Storage cloud │
            │  - Storage local │  │  - Deep-link FRG │
            └──────────────────┘  └──────────────────┘
                  ↑ v1                    ↑ palier 2
```

**v1 ship** : moteur + Host A (FORGE module). Host B est explicitement OUT pour v1.

---

## 4. Le DSL — grammaire

### 4.1 Structure de base d'un statement

```
VERBE <CIBLE> [<MODIFIER>...]
```

- **Verbe** : noyau fermé d'environ 50 verbes au total à terme. Pour v1 (type CLI uniquement), la palette initiale couvre ~20 verbes (voir §6.2). Exemples : `filtrer`, `créer`, `analyser`, `transformer`, `envoyer`, `surveiller`, `valider`, `déclencher`, `enregistrer`, `alerter`, `générer`, `parser`, `scraper`, `recevoir`, `logger`...
- **Cible** `<X>` : slot ouvert (l'user nomme librement) ou token fermé du domaine
- **Modifier** `<Y>` : slot avec qualifier optionnel `<auth:jwt>`, `<db:postgres>`, `<format:json>`...

### 4.2 Chaînage de statements

| Opérateur | Sens |
|---|---|
| `→` | Séquence (A puis B) |
| `&` | Parallèle (A et B simultanément) |
| `\|` | Alternative (A ou B selon condition) |

Exemples :
```
scrape <site:X> → parser <html> → enregistrer <db>
recevoir <webhook:github> → valider <signature> & logger <event>
détecter <erreur> → notifier <slack> | retry <3-fois>
```

### 4.3 Caractères réservés

| Token | Sens |
|---|---|
| `<X>` | Slot ouvert (l'user nomme) |
| `:` | Qualifier (`<auth:jwt>`) |
| `?X` | Incertain — à clarifier avec Claude |
| `!X` | Contrainte non-négociable (`!offline`, `!RGPD`) |
| `@X` | Référence à entité déclarée ailleurs (réutilisation) |
| `#` | Commentaire single-line (strippé avant LLM) |
| `##` | Bloc commentaire (strippé avant LLM) |

### 4.4 Header de projet (3 lignes)

```
TYPE: cli
NAME: mail-filter
GOAL: filtrer mes mails par pertinence
```

### 4.5 Exemple complet de fichier `.dna` source

```
# Projet : filtre intelligent pour ma boîte mail
TYPE: cli
NAME: mail-filter
GOAL: filtrer mes mails par pertinence et les archiver

## Pipeline principal
filtrer <mail> <pertinence>     # filtre principal
  → enregistrer <db:sqlite>     # stockage local !offline
  & alerter <slack> <si:pertinence-haute>

## Edge cases
détecter <erreur:imap> → retry <3-fois> | logger <error.log>
```

### 4.6 Comportement des commentaires

- **Stockage** : tous les `#` sont conservés dans le fichier `.dna` (lisibilité humaine)
- **Transmission LLM** : tous les `#` sont strippés par le `Compressor` avant envoi à Claude
- **Bénéfice** : zéro coût en tokens transmis, mais documentation préservée pour le dev

---

## 5. UI / flow utilisateur

### 5.1 Trois écrans principaux

#### Écran 1 — "Mes projets" (CRUD entry)

- Liste des projets sauvegardés (vignette : nom + type + 1ère ligne du goal)
- Boutons : `+ Nouveau`, `Ouvrir`, `Dupliquer`, `Supprimer`
- Filtre par type de projet
- Tri par date modifiée (par défaut)

#### Écran 2 — "Composer" (le cœur du produit)

Trois zones empilées verticalement :

**Zone haute — Header projet**
```
[ TYPE: ▼ CLI ]   [ NAME: _____ ]   [ GOAL: _________ ]
```
Le choix du TYPE charge la grammaire associée (verbes valides, modifiers pertinents) dans le `TokenDB`.

**Zone centrale — Composer de statements**
Chaque statement = une ligne avec dropdowns en cascade :

```
[verbe ▼]  [<cible> ▼ ou tape]  [+ modifier ▼]  [+ # commentaire]
   ↓          ↓                      ↓
filtrer    <mail>                 <pertinence>
```

- Autocomplete sur les slots `<X>` : suggère les entités déjà déclarées (via `@`)
- Bouton `+ Statement` ajoute une ligne
- Connecteur entre lignes : `→` `&` `|` (cliquable pour changer)
- Boutons ↑↓ pour réordonner (drag & drop = palier 2)
- Champ `#` pour commentaire à droite

**Zone basse — Preview live**
Affiche en temps réel la chaîne DSL compilée :
```
filtrer <mail> <pertinence> → enregistrer <db> & alerter <slack>
```
+ **compteur de tokens estimés** : *« 12 tokens — économie 87% vs prose »*

#### Écran 3 — "Export"

Trois sorties au choix :
- **Copy DSL** → presse-papier, à coller dans n'importe quel LLM
- **Download .dna** → fichier projet complet (header + statements + commentaires)
- **Push to FORGE** → ouvre direct dans la 3D si FORGE est lancé (panel interne en v1, deep-link en palier 2)

### 5.2 Validation continue

Le `Parser` tourne en background, surligne en rouge les statements invalides avec tooltip explicatif. Aucune modal bloquante pendant l'édition — uniquement à l'export.

---

## 6. Mapping DSL → `.dna` → 3D FORGE

### 6.1 Règles de mapping (déterministes)

| Élément DSL | Représentation 3D dans FORGE |
|---|---|
| 1 statement | 1 module cube (primitive FORGE existante) |
| Verbe | Couleur du module (palette par catégorie d'action) |
| Cible `<X>` | Label gros texte sur la face du cube |
| Modifiers | Badges/icônes flottants attachés au cube |
| `→` | Flèche directionnelle pleine (séquence) |
| `&` | Double ligne parallèle |
| `\|` | Pointillés + losange (alternative) |
| `@X` | Ligne fantôme entre modules partageant l'entité |
| `!X` | Halo rouge pulsant (contrainte non-négociable) |
| `?X` | Halo jaune pulsant (à clarifier avec Claude) |
| Commentaire `#` | Label flottant au survol (mode "doc" : tous visibles) |
| Header (TYPE) | Palette/thème global de la scène |
| Header (NAME+GOAL) | Plaque au sol, racine du graphe |

### 6.2 Palette de verbes (initiale, pour CLI)

| Catégorie | Verbes | Couleur |
|---|---|---|
| Acquisition | `scraper`, `recevoir`, `lire` | Bleu |
| Transformation | `filtrer`, `parser`, `transformer` | Cyan |
| Analyse | `analyser`, `détecter`, `valider` | Orange |
| Action | `créer`, `enregistrer`, `générer` | Vert |
| Communication | `envoyer`, `alerter`, `notifier` | Violet |
| Contrôle | `surveiller`, `déclencher`, `retry` | Jaune |
| Erreur | `logger`, `gérer-erreur` | Rouge foncé |

### 6.3 Structure du fichier `.dna` (JSON)

```json
{
  "version": "1.0",
  "header": {
    "type": "cli",
    "name": "mail-filter",
    "goal": "filtrer mes mails par pertinence"
  },
  "statements": [
    {
      "id": "s1",
      "verb": "filtrer",
      "target": "mail",
      "modifiers": [{ "key": null, "value": "pertinence" }],
      "comment": "filtre principal du flow",
      "links": [{ "to": "s2", "type": "seq" }]
    },
    {
      "id": "s2",
      "verb": "enregistrer",
      "target": "db",
      "modifiers": [{ "key": "type", "value": "sqlite" }],
      "constraints": ["offline"],
      "links": [{ "to": "s3", "type": "par" }]
    },
    {
      "id": "s3",
      "verb": "alerter",
      "target": "slack",
      "modifiers": []
    }
  ],
  "rendering": {
    "s1": { "pos": [0, 0, 0] },
    "s2": { "pos": [3, 0, 0] },
    "s3": { "pos": [3, 0, 3] }
  }
}
```

La couche `rendering` est optionnelle : si absente, FORGE auto-layout via algo de graphe orienté.

### 6.4 Bénéfice du format unique

Le **même fichier `.dna`** est :
- **Lu par Claude** (transpilé en chaîne DSL compressée → ~12 tokens)
- **Affiché en 3D dans FORGE** (modules + liens + couleurs)
- **Versionnable Git** (JSON propre, diff-friendly)
- **Éditable à la main** (un dev avancé peut taper le JSON directement)

---

## 7. Composants internes (architecture détaillée)

### 7.1 Les 6 unités du moteur

Chaque unité a **une seule responsabilité** et une **interface propre**.

#### 1. `TokenDB`
- **Responsabilité** : dictionnaire de tokens (verbes, objets, modifiers) + grammaires par type
- **Source** : fichier JSON versionnable (`tokens.json`)
- **Interface** :
  ```
  getVerbsForType(type): Verb[]
  getModifiersForVerb(verb): Modifier[]
  isValidCombination(verb, target, modifier): bool
  ```
- **Pure data + lookup** — aucune logique métier

#### 2. `Composer`
- **Responsabilité** : assemble les choix UI structurés en chaîne DSL valide
- **Interface** :
  ```
  compose({verb, target, modifiers, comment}): Statement
  composeChain([statements], links[]): DSLString
  ```
- **Le seul à pouvoir *écrire* du DSL**

#### 3. `Parser`
- **Responsabilité** : DSL string → AST typé + erreurs
- **Interface** :
  ```
  parse(dsl: string): { ast: AST, errors: ParseError[] }
  ```
- **Usages** : recharger un `.dna` sauvegardé, valider une édition manuelle, surligner les erreurs en live

#### 4. `Transpiler`
- **Responsabilité** : AST → fichier `.dna` (JSON complet)
- **Interface** :
  ```
  transpile(ast: AST): DnaFile
  ```
- **Format de stockage canonique**

#### 5. `Compressor`
- **Responsabilité** : `.dna` → chaîne DSL minimale prête pour Claude (strip commentaires, normalise espaces, supprime metadata visuelle)
- **Interface** :
  ```
  compress(dna: DnaFile): DSLCompressedString
  ```
- **Format "envoi LLM"**

#### 6. `Storage`
- **Responsabilité** : CRUD sur projets
- **Interface unique** :
  ```
  save(dna: DnaFile, name: string): ProjectId
  load(id: ProjectId): DnaFile
  list(): ProjectMetadata[]
  delete(id: ProjectId): void
  ```
- **Implementation différente par host** :
  - FORGE : SQLite local OU fichiers JSON dans le dossier user
  - Web (palier 2) : IndexedDB local + Supabase cloud

### 7.2 Hosts (couches d'intégration)

#### HOST A — FORGE (v1)
- Panel Godot C# qui appelle le moteur
- `RendererAdapter` qui transforme `.dna` → modules 3D Godot (couleurs, halos, liens)
- Storage local

#### HOST B — Web standalone (palier 2)
- SPA légère qui appelle le moteur
- Sortie : copy DSL / download `.dna` / deep-link vers FORGE
- Storage cloud

### 7.3 Choix technique du moteur (à trancher en phase implémentation)

3 options :
- **A. Engine en TypeScript → compilé en Wasm → consommé par Godot (via wasmtime-csharp) ET par le navigateur** ← recommandation actuelle
- **B. Engine en C# → DLL pour FORGE, transpilée en JS via Bridge.NET pour web**
- **C. Engine en deux versions parallèles** (TS pour web, C# pour FORGE) — DRY violation mais plus simple à shipper

**Décision finale renvoyée à la phase de planification d'implémentation.** Mais l'option A garantit zéro drift entre les deux hosts.

---

## 8. Validation, erreurs, tests

### 8.1 Quatre niveaux de validation

1. **Syntaxique (Parser)** — DSL bien formé : `<>` fermées, `:` dans modifier valide, opérateurs bien placés
2. **Sémantique (Composer + TokenDB)** — verbe existe, modifier compatible, type de projet autorise ce verbe
3. **Cohérence cross-statements** — `@X` résolvent vers entités déclarées, pas de cycle non-marqué, pas de contradiction entre `!X`
4. **Render (FORGE only)** — `.dna` rendable, fallback module gris si non

### 8.2 UX des erreurs

- **Pendant la composition** : surlignage rouge live, tooltip explicatif (jamais de modal bloquante)
- **Avant export** : bouton "Export" devient orange si warnings, rouge si erreurs ; tooltip "3 erreurs à corriger"
- **Avant envoi LLM** : modal récap des warnings (l'user peut forcer l'envoi à ses risques)

### 8.3 Stratégie de tests

| Couche | Type | Objectif |
|---|---|---|
| TokenDB | Unit | Lookup déterministe, grammaires bien chargées |
| Composer | Unit | `{choices} → DSL string` reproductible |
| Parser | Unit + property-based (palier 2) | Round-trip DSL ↔ AST sans perte |
| Compressor | Unit | Idempotent, strip commentaires, sortie stable |
| Transpiler | Unit + snapshot | AST → `.dna` conforme schéma JSON |
| Renderer (FORGE) | Snapshot | `.dna` → scène 3D attendue |
| Engine end-to-end | Integration | UI → DSL → `.dna` → 3D, sans régression |
| LLM round-trip | Manuel + benchmark | DSL compressé donne réponse Claude équivalente à prose |

### 8.4 Benchmark continu

Ratio de compression (mots prose / tokens DSL) sur un corpus de **50 briefs réels** issus des sessions historiques de bui1 avec Claude.

**Cible** : ≥ 10×. Si le ratio baisse en dessous, alerte régression.

---

## 9. Scope v1 (MVP shippable)

### IN

- Moteur complet (6 unités au minimum fonctionnel)
- TokenDB seedé avec **un seul type** : **CLI** (grammaire la plus petite, prouve le concept)
- Host A — module FORGE :
  - Panel "Intake" avec composer 3 zones
  - Validation live (rouge en direct)
  - Export : copy DSL + download `.dna` + push 3D
  - CRUD complet sur projets stockés en local
- Tests unit + integration + snapshot rendu FORGE
- Documentation utilisateur (README + 1 exemple end-to-end)

### OUT — palier 2

- ❌ Host B — app web standalone
- ❌ Cloud storage / infra SaaS
- ❌ Multi-types (Web app, Mobile, API, Library, Bot, Script à ajouter après CLI prouvé)
- ❌ Drag & drop reorder (boutons ↑↓ suffisent)
- ❌ Property-based tests
- ❌ Round-trip LLM in-app

### OUT — anti-features (jamais)

- 🚫 IA qui auto-génère le DSL depuis prose → contredit le pitch "économie de tokens"
- 🚫 Plugin system pour verbes custom → casse la grammaire fermée, drift garanti
- 🚫 i18n complet → français + anglais hardcodés suffisent

---

## 10. Naming

**SEED** — *Plant the intent. Grow the system.*

Métaphore : une graine produit le `.dna` qui pousse en 3D dans FORGE. Cohérent avec la mythologie biologique de FORGE (modules-cellules, ADN, écosystème vivant).

Court, prononçable FR/EN, autonome (pas dépendant nominal de FORGE).

---

## 10bis. FORGE Visual Library (couche visuelle proprietary)

**Concept clé** : le DSL et le `.dna` restent **inchangés et open-core**. La richesse visuelle FORGE est une couche supérieure qui consomme le `.dna` standard mais ajoute :

### Ce que la Visual Library apporte

- **Icon library** : une icône (SVG ou pixel art) par verbe (`filtrer`, `enregistrer`, `alerter`...) — affichée sur la face du module 3D, pas juste un label texte
- **Custom 3D module shapes** : par catégorie de verbe, primitive 3D distinctive (au lieu d'un simple cube générique)
  - Acquisition → cube avec antenne
  - Transformation → cube avec engrenages animés
  - Communication → cube avec speaker pulsant
  - Erreur → cube avec halo d'avertissement
- **Themes par TYPE de projet** : palette + skybox + sol + ambiance
  - `cli` → terminal vert sur noir, lignes rétro
  - `webapp` → bleu/blanc clean, néon léger
  - `mobile` → pastel arrondi
  - `api` → gris tech, hexagones
- **Animations contextuelles** : flèches `→` qui pulsent au survol, modules avec halo `?X` qui clignotent, modules `!X` qui vibrent légèrement

### Bénéfices

| Pour | Bénéfice |
|---|---|
| L'user FORGE | Lecture instantanée du graphe sans lire le texte (icônes + couleurs + formes parlent) |
| Le pitch business | Différenciateur visuel non-copiable même si la spec DSL devient open-source |
| L'écosystème | Autres viewers peuvent rendre le `.dna` (texte, mind-map 2D, autre) — FORGE garde l'expérience premium 3D |

### Localisation

Cette couche **ne vit pas dans `seed/`** (qui reste pur engine). Elle vit dans **`forge/src/Forge.Godot/Visual/SeedRenderer/`** :

```
forge/src/Forge.Godot/Visual/SeedRenderer/
├── IconLibrary/                  ← 1 SVG par verbe
├── ModuleShapes/                 ← 1 .tscn par catégorie
├── Themes/                       ← 1 .tres par TYPE
└── DnaToScene.cs                 ← consume .dna → instancie tout
```

### Modèle économique

C'est le **vrai moat de FORGE** :

- **Spec DSL + engine + `.dna`** : open-core (Apache 2.0, palier 2)
- **FORGE Visual Library** : proprietary, vendue avec FORGE
- **Tu peux lire du `.dna` dans n'importe quel viewer**, mais l'expérience premium reste FORGE (comme PDF est ouvert mais Adobe Reader reste référence)

### Scope

**OUT de v1 SEED** (pas dans le plan engine actuel). Sera traité dans le **plan FORGE integration** qui suit l'engine v0.1.0.

---

## 11. Open questions / décisions reportées à l'implémentation

1. **Tech stack moteur** : Wasm (TS) vs C# vs dual implementation
2. **Storage v1** : SQLite vs JSON files dans dossier user
3. **Liste exacte des verbes du noyau CLI** (à figer avant codage du TokenDB)
4. **Algorithme d'auto-layout 3D** quand `rendering` absent (graph-viz simple ? force-directed ? hiérarchique ?)
5. **Persona du panel "Intake" dans FORGE** : panel autonome ou intégré au workshop existant ?
6. **Format de l'export `.dna`** : JSON pur ou JSON+commentaires inline préservés ?

---

## 12. Décisions actées (résumé du brainstorming)

| Décision | Choix |
|---|---|
| Target user v1 | bui1 uniquement |
| Architecture déploiement | FORGE module + web standalone (v1 = FORGE seulement) |
| Moteur de questions | DSL avec DB de tokens (option E composée) |
| Vocabulaire | Hybride : noyau fermé + slots ouverts |
| Entry flow UX | Hybride A→C : pick type → compose token par token |
| Commentaires | `#` et `##`, strippés avant envoi LLM |
| Output primaire | Chaîne DSL compressée |
| Output secondaire | Fichier `.dna` JSON pour 3D |
| Type initial v1 | CLI |
| Nom | SEED |

---

## Prochaine étape

Invocation de la skill `superpowers:writing-plans` pour produire un plan d'implémentation détaillé avec phases, livrables, et tests par étape.
