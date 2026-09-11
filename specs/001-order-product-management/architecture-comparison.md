# Architecture Comparison: Monorepo Tooling & Vue.js API Style

**Related**: [architecture.md](./architecture.md) (chosen architecture) · [research.md](./research.md) (decision log)

This document lays out the alternatives considered for two choices baked into `architecture.md` and `plan.md` — the monorepo build tool for `frontend/`, and the Vue.js component API style used in `product-app` — and why each was decided the way it was. It exists as a standalone decision record so the reasoning survives independently of the (already-implemented) outcome.

---

## 1. Monorepo Architecture: Nx vs. Turborepo

**Context**: `frontend/` is a single workspace containing two independently deployable apps on two different frameworks — `order-app` (Angular) and `product-app` (Vue) — plus shared libraries (`libs/shared/models`, `libs/shared/api-clients`, `libs/shared/util`). The tool needs to build, serve, test, and lint both apps and keep the shared libraries wired to both via TypeScript path aliases.

### Comparison

| Criterion | **Nx** | **Turborepo** |
|---|---|---|
| Multi-framework scaffolding | First-party generators for both frameworks used here: `@nx/angular:app`, `@nx/vue:app`, plus `@nx/js:lib` for framework-agnostic shared libs. One command each, wired into the workspace graph automatically. | No official app/framework generators. You bring your own Angular CLI / Vite setup per app and wire Turborepo's `turbo.json` pipeline around them by hand. |
| Task orchestration & caching | Built-in computation cache (local + optional remote), dependency-graph-aware task scheduling (`nx affected`, `nx run-many`), and a visual dependency graph (`nx graph`). | Also has local + remote caching and a `turbo.json` pipeline — comparable caching model, but no built-in project graph visualization and less opinionated about *how* projects relate to each other. |
| Code generation / scaffolding | Generators (`nx g ...`) for apps, libs, components — used throughout this project to scaffold `order-app`, `product-app`, and the shared libs consistently. | None built in; relies entirely on each framework's own CLI (`ng generate`, `vue create`, etc.) run manually per package. |
| Editor/IDE tooling | Nx Console (VS Code/IntelliJ) gives a UI over generators and the project graph. | Lighter-weight; no equivalent first-party IDE plugin ecosystem for scaffolding. |
| Enforcing architectural boundaries | `nx.json` + ESLint module-boundary rules can enforce, e.g., "no app may import another app's internals" — relevant here since `order-app` and `product-app` must only share `libs/shared/*`, never each other. | No built-in equivalent; boundary enforcement would need custom ESLint config maintained separately. |
| Learning curve / footprint | More opinionated, more moving parts (generators, executors, plugins) — a heavier tool for a two-app workspace. | Minimal, close to "just npm workspaces + a task runner" — easier to reason about for a small number of packages. |
| Fit for this project | Directly matches the requested "MonoRepo (Order - Angular, Product - Vue)" structure with first-party support for exactly these two frameworks in one graph. | Would work, but every piece of framework-specific tooling (Angular's build system, Vite for Vue) has to be assembled manually rather than generated. |

### Decision: **Nx** (as implemented)

**Rationale**: This project's defining constraint — two different frontend frameworks in one workspace, sharing framework-agnostic libraries — is exactly the case Nx's `@nx/angular` and `@nx/vue` plugins are built for. Scaffolding (`npx nx g @nx/angular:app apps/order-app`, `npx nx g @nx/vue:app apps/product-app`) produced correctly wired, independently buildable/servable apps in one step, and `libs/shared/*` are consumed by both through the same `tsconfig.base.json` path-mapping mechanism Nx expects. Turborepo would have been a reasonable choice for a *same-framework* multi-app monorepo (e.g., two Next.js apps), where its lighter footprint is an advantage — but it offers no framework-specific scaffolding, so the Angular and Vite/Vue build setups would have had to be hand-assembled and kept in sync manually, with no equivalent of Nx's module-boundary lint rule to keep `order-app` and `product-app` from accidentally importing each other's internals.

**When to revisit**: If the workspace grows to primarily same-framework apps (e.g., several Vue micro-frontends) where Nx's Angular-specific tooling stops pulling weight, or if Nx's larger dependency footprint becomes a build-time concern, Turborepo becomes the more attractive lighter-weight option.

---

## 2. Vue.js Component API Style: Options API vs. Composition API

**Context**: `product-app`'s views (`ProductList.vue`, `ProductForm.vue`, `ProductDetail.vue`) and `App.vue` need to call the shared `ProductApiClient`, manage local component state (search query, form fields, loading/error flags), and integrate with Pinia for cross-component auth/product state.

### Comparison

| Criterion | **Options API** | **Composition API** |
|---|---|---|
| Code organization | Groups code by *option type* (`data`, `methods`, `computed`) — related logic for one feature ends up scattered across multiple blocks. | Groups code by *feature/concern* — everything for, say, "search" (state + the function that updates it) lives together, which is how this project's views are written (`<script setup>`). |
| TypeScript inference | Works, but `this`-based typing inside options historically needed more type-annotation ceremony to get fully correct inference. | Inference is direct and precise — `ref<Product[]>([])`, `computed(() => ...)` all type-check naturally, which matters given the whole stack here (`libs/shared/models`, `libs/shared/api-clients`) is TypeScript-first. |
| Logic reuse across components | Mixins (legacy) or scoped slots — mixins have well-known naming-collision and "where did this property come from" traceability problems. | Composable functions (e.g., a `useProductSearch()` could be extracted) — plain functions, explicit imports, no implicit merging. Not yet needed in `product-app` (only three views), but the option exists if the catalog UI grows. |
| `<script setup>` ergonomics | Not applicable — Options API doesn't have a `<script setup>` form. | `<script setup lang="ts">` (used throughout `product-app`) eliminates the boilerplate `export default { setup() { ... return {...} } }` wrapper; top-level bindings are automatically exposed to the template. |
| Pinia integration | Pinia supports both API styles for consuming stores. | Also fully supported; `product-app`'s stores (`auth.store.ts`, `product.store.ts`) use Pinia's own Options-style store definition (`defineStore('id', { state, actions })`) internally — note Pinia's *store definition* syntax and the *component* API style are independent choices; this project uses the Options-style Pinia store definition (simple, one file, matches the small state shape here) together with the Composition API in components. | 
| Alignment with Vue 3 / ecosystem direction | Still fully supported and not deprecated, but Vue's own docs, RFCs, and most new library integrations (VueUse, etc.) are written Composition-API-first. | Vue core team's primary recommendation since Vue 3; new ecosystem tooling assumes it by default. |
| Learning curve for teams new to Vue | Often considered slightly more approachable initially — closer to "one object with named sections," similar to older Vue 2 code many tutorials still reference. | Slightly more to learn upfront (`ref` vs. `reactive`, `.value` unwrapping), but pays off quickly once a component has more than a couple of pieces of related state/logic. |

### Decision: **Composition API** (as implemented)

**Rationale**: Every view in `product-app` is written with `<script setup lang="ts">` (see `ProductList.vue`, `ProductForm.vue`, `ProductDetail.vue`, `App.vue`). Two things drove this:

1. **TypeScript-first stack**: `libs/shared/models` and `libs/shared/api-clients` are typed, and the Composition API's `ref<T>`/`computed<T>` give direct, correct inference against those types without extra annotation work — a meaningful win given the shared-library-heavy architecture in `architecture.md` §2.1.
2. **Feature-oriented grouping**: Views like `ProductDetail.vue` mix data-loading (`load()`), editing state (`editing`, `editPrice`, `editStock`), and actions (`saveEdits`, `retire`) — Composition API keeps each concern's state and behavior adjacent and readable in one place, rather than splitting them across `data()`/`methods`/`computed` blocks.

**When to revisit**: If `product-app` were being built by a team with deep existing Vue 2/Options API muscle memory and no near-term need for composable logic reuse, Options API remains a fully supported, valid choice — this was not a case of Composition API being "correct" and Options API being "wrong," just the better fit for this codebase's TypeScript-heavy, shared-library-driven shape.

---

## Summary

| Decision | Chosen | Runner-up | Primary reason |
|---|---|---|---|
| Monorepo tool | Nx | Turborepo | First-party Angular *and* Vue app generators in one graph, plus module-boundary enforcement between `order-app` and `product-app` |
| Vue API style | Composition API (`<script setup>`) | Options API | Best TypeScript inference against the shared typed models/clients; keeps per-feature state and logic together |
