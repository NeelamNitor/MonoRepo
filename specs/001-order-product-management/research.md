# Phase 0 Research: Order and Product Management Platform

**Input**: Technical Context in [plan.md](./plan.md) | **Output**: Decisions consumed by Phase 1 (`data-model.md`, `contracts/`, `quickstart.md`)

None of the items below were left as `[NEEDS CLARIFICATION]` in `plan.md` — the mandated tech stack (Angular, Vue, Nx monorepo, .NET Core microservices, CQRS/MediatR, Clean Architecture, Event-Driven, Repository, Singleton, PostgreSQL) was fixed by the user's request. This document instead resolves the *supporting* technical decisions that stack implies but doesn't specify, plus the open questions carried over from `project-details.md` §9.

## 1. Event Bus / Message Broker

**Decision**: RabbitMQ (via MassTransit as the .NET abstraction layer), self-hosted or managed.

**Rationale**: The event-driven integration in `architecture.md` §3.4 needs reliable at-least-once delivery for a small, fixed set of event types (`StockReservationRequested/Reserved/Rejected`, `OrderCancelled`, `OrderStatusChanged`, `ProductRetired`) between exactly two services — this is a topology RabbitMQ handles well without the operational overhead of a log-based streaming platform. MassTransit gives typed publish/consume, outbox support (needed to publish events transactionally with the EF Core unit of work), and retry/dead-letter handling out of the box, which keeps the Infrastructure-layer messaging code in each service thin.

**Alternatives considered**:
- **Kafka** — better fit for high-throughput event streaming/replay across many consumers; unnecessary operational complexity for two services and a handful of event types at the SC-005 scale target (100k orders).
- **Azure Service Bus** — a strong choice if the deployment target is confirmed Azure; deferred until hosting platform is decided, since RabbitMQ keeps the plan cloud-agnostic.

## 2. Transactional Outbox for Domain Events

**Decision**: Use the transactional outbox pattern (event written to an `OutboxMessage` table in the same PostgreSQL transaction as the aggregate change, then relayed to RabbitMQ by a background dispatcher) in both services.

**Rationale**: FR-005/FR-007 require stock reservation/release to be reliable and race-free; publishing an event as a side effect of a command handler without an outbox risks the event being lost if the process crashes after the DB commit but before the publish. MassTransit has built-in EF Core outbox support, so this adds no new library, only a migration and DI registration per service.

**Alternatives considered**: Direct publish-after-commit (simpler, but not crash-safe) — rejected given FR-005's "no overselling" success criterion (SC-003) implies the stock-adjustment event path must be reliable.

## 3. Atomic Stock Decrement (FR-005, SC-003)

**Decision**: Enforce atomicity at the database layer inside `ProductRepository.ReserveStockAsync`, using a single conditional UPDATE (`UPDATE products SET stock = stock - @qty WHERE id = @id AND stock >= @qty`) and checking rows-affected, combined with EF Core's row-version optimistic concurrency token as a defense-in-depth check.

**Rationale**: A conditional `UPDATE ... WHERE stock >= @qty` is atomic at the PostgreSQL row level regardless of how many concurrent requests hit it — no explicit application-level lock is needed for correctness, only for good error messaging. This is simpler and more scalable than pessimistic locking (`SELECT ... FOR UPDATE`) or a distributed lock, while still satisfying "no overselling in 100% of concurrent-order test scenarios."

**Alternatives considered**:
- **`SELECT ... FOR UPDATE`** — correct but serializes all stock updates for a product row for the duration of the transaction; unnecessary given the conditional-UPDATE approach achieves the same guarantee without holding a lock across a round trip.
- **Distributed lock (e.g., Redis-based)** — needed only if stock updates must coordinate across multiple databases/processes outside a single PostgreSQL transaction, which is not the case here (Product Service owns `product_db` exclusively). Not used for correctness; the Singleton-scoped coordinator mentioned in `architecture.md` §3.6 is retained only as an in-process guard against redundant concurrent reservation attempts for the same order, not as the source of correctness.

## 4. API Gateway / BFF

**Decision**: No dedicated API Gateway for v1. Each frontend app calls its corresponding service directly (`order-app` → Order Service, `product-app` → Product Service) over HTTPS; a reverse proxy (e.g., simple Nginx/YARP config) handles routing/TLS termination only, with no business logic.

**Rationale**: With exactly two frontends and two services in a 1:1 relationship, a full Gateway/BFF layer adds a deployable, a hop, and an on-call surface without solving a problem that exists yet (no aggregation of multiple services into one frontend call is required by any user story). `architecture.md` §1 already marks this component optional.

**Alternatives considered**: Introduce a Gateway later if/when a single frontend view needs data aggregated from both services (e.g., a combined dashboard) — revisit at that point rather than pre-building it.

## 5. Identity Provider / Authentication (FR-012)

**Decision**: OAuth2/OIDC via an external identity provider (e.g., Auth0, Azure AD/Entra ID, or Keycloak if self-hosted is required) issuing JWT bearer tokens validated independently by both microservices; role claims in the token drive the "catalog management restricted to authorized roles" check in FR-012.

**Rationale**: Standard, framework-supported (`Microsoft.AspNetCore.Authentication.JwtBearer`) approach that keeps both services stateless with respect to auth and avoids either service owning user/credential storage.

**Alternatives considered**: Custom-built auth service — rejected as unnecessary scope for this feature; spec's Assumptions section already defers identity-provider selection.

## 6. Nx Monorepo with Mixed Angular + Vue Apps

**Decision**: Single Nx workspace at `frontend/`, using `@nx/angular` for `order-app` and `@nx/vue` for `product-app`, with `libs/shared/*` built as framework-agnostic TypeScript libraries (no Angular or Vue imports).

**Rationale**: Nx explicitly supports multiple frontend frameworks in one workspace with per-project build/lint/test targets and an affected-graph that avoids cross-app rebuilds — this is exactly the isolation `architecture.md` §2.1 requires (each app builds/deploys independently). Keeping `libs/shared/models` and `libs/shared/api-clients` framework-agnostic is what makes sharing safe without coupling the two apps' dependency trees.

**Alternatives considered**: Two separate repositories (one per frontend) — rejected because it would lose the shared-models/shared-API-client benefit and require a separate versioning/publishing step just to share DTOs that change alongside the backend contracts.

## 7. CQRS + MediatR Pipeline Behaviors

**Decision**: Standard MediatR `IPipelineBehavior<TRequest,TResponse>` chain per service: `ValidationBehavior` (FluentValidation, enforces FR-011) → `LoggingBehavior` → `UnitOfWorkBehavior` (commits EF Core `SaveChangesAsync` + outbox flush after a Command handler succeeds; skipped for Queries).

**Rationale**: This is the idiomatic MediatR pattern for CQRS in Clean Architecture .NET services — cross-cutting concerns stay out of individual handlers, and Commands vs. Queries are visibly distinguished by which behaviors apply to them (Queries never go through `UnitOfWorkBehavior` since they must not mutate state).

**Alternatives considered**: Manual validation/transaction-handling inside each handler — rejected as repetitive and error-prone (easy to forget in a new handler) compared to a pipeline behavior applied uniformly.

## 8. Repository Pattern Scope

**Decision**: One repository interface per aggregate root, defined in `Application` (or `Domain`, per team convention — either is acceptable Clean Architecture; this plan uses `Domain` since the interface expresses a domain concept), implemented in `Infrastructure` with EF Core. No generic `IRepository<T>` — each repository exposes only the methods its handlers actually need (e.g., `IOrderRepository.GetByIdAsync`, `AddAsync`; `IProductRepository.ReserveStockAsync`, `SearchAsync`).

**Rationale**: A generic repository tends to leak IQueryable/EF-specific concerns back up into the Application layer, defeating the purpose of the pattern (isolating persistence). Purpose-built methods per aggregate keep the Application layer testable against simple fakes and keep `ReserveStockAsync`'s atomicity guarantee (research item 3) encapsulated in one place.

**Alternatives considered**: Generic repository + specification pattern — more flexible for ad hoc queries, but unnecessary complexity for the fixed set of query shapes FR-001–FR-011 require; can be introduced later if query variety grows.

## 9. Singleton-Scoped Services

**Decision**: Register as `Singleton` in each service's DI container: the RabbitMQ/MassTransit bus connection, the `IOptions<T>`-backed configuration accessor, and (per `architecture.md` §3.6) the in-process reservation coordinator used as a defense-in-depth guard alongside the conditional-UPDATE approach in research item 3. Everything else (DbContext, repositories, MediatR handlers) stays `Scoped`/`Transient` per ASP.NET Core convention.

**Rationale**: These three are stateless or hold one process-lifetime connection/resource that is expensive to recreate per request; everything touching the database or a single request's data must remain request-scoped to avoid cross-request state leakage (a documented pitfall of over-applying Singleton).

**Alternatives considered**: Singleton `DbContext` — explicitly rejected; EF Core's `DbContext` is not thread-safe and must be `Scoped`.

## 10. PostgreSQL Access — EF Core Migrations Strategy

**Decision**: Code-First EF Core migrations, one migration history per service (`OrderService.Infrastructure/Persistence/Migrations`, `ProductService.Infrastructure/Persistence/Migrations`), applied via each service's own CI/CD pipeline at deploy time (`dotnet ef database update` or an EF Core migration bundle) — never a shared migration project.

**Rationale**: Matches database-per-service; keeps each service capable of evolving its schema independently, consistent with the project's goal of independent releasability (`project-details.md` §2).

**Alternatives considered**: A shared "Persistence" project with all migrations — rejected, would recreate the coupling database-per-service is meant to avoid.

## Summary of Resolved Unknowns

| Open Question (from `project-details.md` §9) | Resolution |
|---|---|
| Event bus/message broker | RabbitMQ + MassTransit (transactional outbox) |
| API Gateway / BFF | Not used in v1; reverse proxy only |
| Identity provider | OAuth2/OIDC, specific vendor deferred to deployment decision, JWT bearer validated in both services |

All Technical Context fields in `plan.md` are now decision-backed; Phase 1 design can proceed.
