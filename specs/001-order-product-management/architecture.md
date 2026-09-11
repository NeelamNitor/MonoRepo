# Architecture: Order and Product Management Platform

**Feature**: [`spec.md`](./spec.md) | **Created**: 2026-09-04 | **Status**: Draft

## 1. Overview

The platform is split along the same seam on both frontend and backend: **Product** (catalog) and **Order** (purchasing/fulfillment) are two independently owned, independently deployable slices that communicate over well-defined APIs and events. This mirrors the two bounded contexts identified in the spec and lets each side use the frontend framework and release cadence that suits it best, without coupling their codebases or deployments.

```text
┌─────────────────────────────── Nx Monorepo (frontend) ───────────────────────────────┐
│                                                                                        │
│   apps/order-app (Angular)              apps/product-app (Vue.js)                     │
│         │                                        │                                    │
│         └───────────────┬────────────────────────┘                                    │
│                 libs/ (shared models, API clients, UI kit, design tokens)              │
└───────────────────────────────────────┬──────────────────────────────────────────────┘
                                         │ HTTPS / REST (+ SSE or WebSocket for status push)
                          ┌──────────────┴──────────────┐
                          │        API Gateway / BFF      │  (optional, routes to services)
                          └──────┬──────────────┬────────┘
                                 │              │
                 ┌───────────────▼───┐   ┌──────▼────────────┐
                 │  Order Service     │   │  Product Service   │   .NET Core microservices
                 │  (.NET Core)       │   │  (.NET Core)        │
                 │  CQRS + MediatR    │   │  CQRS + MediatR     │
                 │  Clean Architecture│   │  Clean Architecture │
                 └───────┬───────────┘   └──────────┬─────────┘
                         │  publishes/consumes        │
                         │  domain events              │
                         └───────────┬─────────────────┘
                                 ┌────▼─────┐
                                 │  Event Bus │  (e.g., message broker)
                                 └────┬─────┘
                         ┌────────────┴────────────┐
                    ┌────▼─────┐              ┌─────▼────┐
                    │ order_db │  PostgreSQL   │ product_db│  PostgreSQL
                    └──────────┘  (per service)└──────────┘
```

## 2. Frontend Architecture

### 2.1 Nx Monorepo Layout

A single Nx workspace hosts both frontend apps and their shared code, giving one repository, dependency graph, and CI pipeline while keeping each app's framework and build isolated:

```text
frontend/
├── apps/
│   ├── order-app/            # Angular application — Order management UI
│   │   ├── src/app/
│   │   │   ├── features/     # place-order, order-list, order-detail, order-status
│   │   │   ├── core/         # http interceptors, guards, app-level services
│   │   │   └── shared/       # app-local reusable UI
│   │   └── project.json
│   │
│   └── product-app/          # Vue.js application — Product catalog UI
│       ├── src/
│       │   ├── views/        # ProductList, ProductDetail, ProductForm
│       │   ├── components/
│       │   └── stores/       # Pinia/Vuex state
│       └── project.json
│
└── libs/
    ├── shared/models/        # TypeScript interfaces for Product, Order, DTOs (framework-agnostic)
    ├── shared/api-clients/   # Typed HTTP clients per service, generated from OpenAPI
    ├── shared/ui-tokens/     # Design tokens / CSS variables shared by both apps
    └── shared/util/          # Framework-agnostic helpers (formatting, validation)
```

**Why this shape**: `libs/shared/*` contains nothing Angular- or Vue-specific — it is plain TypeScript — so both apps depend on it without pulling in the other framework. Each app builds, tests, and deploys independently (`nx build order-app`, `nx build product-app`), and Nx's affected-graph means a change to `product-app` never triggers an `order-app` rebuild.

### 2.2 Why Angular for Order, Vue for Product

- Order management is the more interaction-heavy, form/state-intensive workflow (multi-step order placement, status tracking); Angular's opinionated structure (DI, RxJS, reactive forms) suits that complexity.
- Product catalog management is comparatively simpler CRUD + search/filter UI, where Vue's lighter footprint and faster iteration speed are a good fit.
- Both are independent SPAs — there is no requirement for them to share a runtime, only to share contracts (`libs/shared/models`) and visual language (`libs/shared/ui-tokens`).

## 3. Backend Architecture

### 3.1 Microservice Boundaries

Two bounded-context microservices, one per business capability:

- **Product Service** — owns product/catalog data and stock levels. Source of truth for FR-001, FR-002, FR-010 (product side), FR-011 (product side).
- **Order Service** — owns order lifecycle data. Source of truth for FR-003, FR-004, FR-006, FR-007, FR-008, FR-010 (order side).

Each service owns its own PostgreSQL database (**database-per-service**) — no service reaches into another's schema. Cross-service data needs (e.g., Order Service needing current product price/name to build a line item) are satisfied via synchronous API calls at order-placement time and via domain events for anything that must stay eventually consistent afterward (e.g., stock adjustments).

### 3.2 Clean Architecture (per service)

Every microservice follows the same four-layer structure so business logic never depends on frameworks or infrastructure:

```text
src/
├── ProductService.Domain/            # Entities, value objects, domain events, domain services
│   ├── Entities/                     #   Product (with invariants: no negative price/stock)
│   ├── Events/                       #   ProductStockReserved, ProductRetired, ...
│   └── Interfaces/                   #   IProductRepository (contract only, no implementation)
│
├── ProductService.Application/       # Use cases — CQRS Commands/Queries + handlers
│   ├── Commands/
│   │   ├── CreateProduct/            #   CreateProductCommand, Handler, Validator
│   │   └── RetireProduct/
│   ├── Queries/
│   │   ├── SearchProducts/
│   │   └── GetProductById/
│   └── Common/                       #   Behaviors: validation, logging (MediatR pipeline)
│
├── ProductService.Infrastructure/    # EF Core DbContext, Repository implementations,
│   ├── Persistence/                  #   PostgreSQL access, migrations
│   ├── Repositories/                 #   ProductRepository : IProductRepository
│   └── Messaging/                    #   Event bus publisher/consumer implementation
│
└── ProductService.Api/               # ASP.NET Core Web API — controllers/minimal APIs
    └── Endpoints/                    #   Thin: build a Command/Query, send via MediatR, return result
```

**Dependency rule**: `Api → Application → Domain`, and `Infrastructure → Application/Domain` (implements their interfaces). Domain has zero dependencies on the other layers. This is what keeps stock-validation rules (FR-005) and order status-transition rules (FR-006) unit-testable without a database or HTTP context.

### 3.3 CQRS + MediatR

Every state change and every read is modeled explicitly:

| Type | Example | Flow |
|---|---|---|
| Command | `PlaceOrderCommand` | `Api` builds command from request → `MediatR.Send()` → `PlaceOrderCommandHandler` (Application layer) loads aggregate via repository, applies domain rules, persists, raises domain events. |
| Query | `SearchProductsQuery` | `Api` builds query from query-string → `MediatR.Send()` → `SearchProductsQueryHandler` reads (optionally via a read-optimized path/projection) and returns a DTO — never touches write-side domain entities. |

MediatR pipeline behaviors implement cross-cutting concerns uniformly: `ValidationBehavior` (FluentValidation, enforces FR-011), `LoggingBehavior`, and `UnitOfWorkBehavior` (commits the EF Core transaction after a command handler succeeds).

### 3.4 Event-Driven Integration

Domain events raised inside a service are published to an event bus after the originating transaction commits, and consumed asynchronously by the other service:

| Event | Published by | Consumed by | Purpose |
|---|---|---|---|
| `StockReservationRequested` → `StockReserved` / `StockRejected` | Order Service → Product Service → Order Service | Product Service, then Order Service | Atomic, race-free stock decrement across service boundaries (FR-005). |
| `OrderCancelled` | Order Service | Product Service | Releases previously reserved stock (FR-007). |
| `OrderStatusChanged` | Order Service | Notification consumer(s) | Satisfies FR-009 — any interested system subscribes without Order Service knowing who they are. |
| `ProductRetired` | Product Service | Order Service (reference cache, if used) | Lets Order Service continue to render historical order line items correctly (Edge Case: retired product referenced by past orders). |

This keeps the two services loosely coupled: Product Service never calls into Order Service synchronously, and vice versa, except for the narrow, synchronous "get current product price/availability" read used at order-placement time.

### 3.5 Repository Pattern

Each service defines repository interfaces in its Application/Domain layer (e.g., `IOrderRepository`, `IProductRepository`) and implements them in Infrastructure using EF Core against PostgreSQL. This:

- Keeps Application-layer command/query handlers unit-testable against an in-memory fake repository.
- Isolates all PostgreSQL/EF Core-specific code (query composition, migrations) in one place per service.
- Allows the atomic stock-decrement requirement (FR-005) to be implemented once, correctly (e.g., optimistic concurrency token or `SELECT ... FOR UPDATE`-style guard inside `ProductRepository.DecrementStockAsync`), rather than scattered across handlers.

### 3.6 Singleton Pattern

Applied narrowly, to process-lifetime, stateless or expensive-to-construct services registered in each service's DI container as `Singleton`:

- Event-bus connection/publisher (one connection per process, reused by all handlers).
- Configuration/options accessor.
- The distributed lock / concurrency-coordination service backing the atomic stock check in FR-005 (e.g., a wrapper around a distributed lock provider), so all requests in the process share one coordinator instance rather than creating one per request.

Domain entities, DbContext, and anything request-scoped or holding per-request state are explicitly **not** singletons (DbContext is registered `Scoped`, per ASP.NET Core/EF Core convention) — the Singleton pattern here is intentionally limited to true cross-cutting infrastructure.

### 3.7 Data Layer — PostgreSQL

- One PostgreSQL database per microservice (`order_db`, `product_db`) — no shared schema, no cross-database joins.
- EF Core Code-First migrations per service, run independently as part of each service's own deployment pipeline.
- `Order Line Item` and `Order Status History` (see spec Key Entities) live in `order_db`; they store a denormalized snapshot of product name/price at order time, precisely so the Order Service never needs a live join into `product_db` to render historical orders (satisfies the "retired product referenced by past order" edge case without cross-service coupling).

## 4. Request Flow Example — Place an Order (User Story 2)

1. User submits order in **order-app** (Angular) → HTTP `POST /orders` to **Order Service**.
2. `Api` layer builds `PlaceOrderCommand`, sends via MediatR.
3. `PlaceOrderCommandHandler` (Application layer) validates input (`ValidationBehavior`), then calls Product Service synchronously to confirm current price/availability for each line item.
4. Handler publishes `StockReservationRequested` event(s) and, on receiving `StockReserved` confirmation (or performing a synchronous reservation call, depending on latency requirements), persists the `Order` aggregate (status `Placed`) via `IOrderRepository`.
5. On successful commit, `OrderStatusChanged` (Placed) event is published for downstream notification consumers (FR-009).
6. Response returns to **order-app**, which reflects the new order and status immediately; subsequent status changes (Story 3) update the same view via polling or a push channel (SSE/WebSocket) fed by `OrderStatusChanged`.

## 5. Cross-Cutting Concerns

- **Authentication/Authorization**: Both frontends attach a bearer token (OAuth2/OIDC) to API calls; each microservice validates the token and enforces role checks at the `Api`/MediatR pipeline level (FR-012), independent of the other service.
- **Validation**: FluentValidation validators run as a MediatR pipeline behavior ahead of every command handler, uniformly enforcing FR-011 across both services.
- **Observability**: Structured logging and correlation IDs propagated from frontend → API Gateway → services → event bus, so an order's full lifecycle (including cross-service event hops) can be traced end-to-end.
- **API contracts**: Each service publishes an OpenAPI spec; `libs/shared/api-clients` in the frontend monorepo is generated from those specs, keeping frontend and backend contracts in sync.

## 6. Deployment Topology (indicative)

- `order-app` and `product-app` build to independent static bundles, deployable to any static host/CDN, each behind its own route (or the API Gateway also serves as the single entry point/reverse proxy for routing to the right SPA).
- `Order Service` and `Product Service` are independently containerized and deployed (e.g., one container/pod per service), scaled independently based on their own load profiles.
- `order_db` and `product_db` are independent PostgreSQL instances/schemas, each owned and migrated only by its service.
